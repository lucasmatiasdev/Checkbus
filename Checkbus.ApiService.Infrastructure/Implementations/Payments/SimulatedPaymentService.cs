using System.Collections.Concurrent;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Domain.ValueObjects;

namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// One in-memory simulated order, as created by <see cref="SimulatedPaymentService.CreateOrderAsync"/>
    /// and read/updated by <see cref="SimulatedCheckoutController"/> (odd/tasks/pagos-mercadopago.md
    /// Fase J1/J2). An immutable snapshot — <see cref="ISimulatedOrderStore.SetStatus"/> replaces
    /// the dictionary entry with a new record (<c>with</c> expression) rather than mutating a
    /// shared instance, to stay trivially thread-safe alongside the backing
    /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    public sealed record SimulatedOrderRecord(
        string OrderId,
        MercadoPagoOrderStatus Status,
        Money TotalAmount,
        string ExternalReference,
        string? SuccessUrl,
        string? FailureUrl,
        string? PendingUrl);

    /// <summary>
    /// Narrower read/write surface over <see cref="SimulatedPaymentService"/>'s in-memory order
    /// store, implemented by that same class (odd/tasks/pagos-mercadopago.md Fase J1) —
    /// <see cref="SimulatedCheckoutController"/> (J2) depends on this interface rather than on the
    /// full <see cref="IPaymentService"/> Application-facing port, to avoid a presentation-layer
    /// dependency on Application. Registered as the SAME singleton instance as
    /// <see cref="IPaymentService"/> (see <c>Program.cs</c>'s J3 wiring) so both interfaces share
    /// one in-memory dictionary across the separate HTTP requests the simulated flow spans.
    /// </summary>
    public interface ISimulatedOrderStore
    {
        /// <summary>Null if <paramref name="orderId"/> was never created by this service — mirrors
        /// how a real 404 from Mercado Pago would surface.</summary>
        SimulatedOrderRecord? Get(string orderId);

        /// <summary>No-op if <paramref name="orderId"/> is unknown — callers (the checkout
        /// controller) already render a "no encontrado" page for that case before ever calling
        /// this.</summary>
        void SetStatus(string orderId, MercadoPagoOrderStatus status);
    }

    /// <summary>
    /// <see cref="IPaymentService"/> adapter that fakes the entire Mercado Pago gateway call with
    /// in-memory state (odd/tasks/pagos-mercadopago.md Fase J, the I10 KYC-blocker workaround) —
    /// everything AROUND this port (hold → order → checkout redirect → webhook → signature
    /// validation → dedupe → confirm/refund, Fases A-I) still runs for real; only the actual
    /// outbound HTTP call to Mercado Pago is replaced. <see cref="SimulatedCheckoutController"/>
    /// (J2) is what turns this fake order into a real, correctly-signed webhook POST back into
    /// this same app's <c>PaymentsWebhookController</c>, so the rest of the domain flow is
    /// exercised for real, not bypassed.
    ///
    /// <para><b>Lifetime: registered SINGLETON</b> (see <c>Program.cs</c>, Fase J3) — deliberately
    /// NOT scoped like <see cref="MercadoPagoPaymentService"/>, because its
    /// <see cref="ConcurrentDictionary{TKey,TValue}"/> must survive across the separate HTTP
    /// requests the simulated flow spans (order-creation request, checkout-page GET, approve/
    /// reject POST, webhook POST) — a scoped/per-request instance would lose every order the
    /// moment the creating request ended.</para>
    /// </summary>
    public class SimulatedPaymentService(SimulatedPaymentOptions options) : IPaymentService, ISimulatedOrderStore
    {
        private readonly ConcurrentDictionary<string, SimulatedOrderRecord> _orders = new();

        public Task<CreateOrderResult> CreateOrderAsync(
            CreateOrderRequest request, CancellationToken cancellationToken, string sellerAccessToken = "")
        {
            var orderId = $"SIM-{Guid.NewGuid()}";

            var record = new SimulatedOrderRecord(
                orderId,
                MercadoPagoOrderStatus.Pending,
                request.TotalAmount,
                request.ExternalReference,
                request.SuccessUrl,
                request.FailureUrl,
                request.PendingUrl);

            _orders[orderId] = record;

            var checkoutUrl = $"{options.ApiBaseUrl.TrimEnd('/')}/api/public/payments/simulate/{orderId}";

            return Task.FromResult(new CreateOrderResult(orderId, checkoutUrl));
        }

        public Task<OrderStatusResult> GetOrderAsync(
            string externalOrderId, CancellationToken cancellationToken, string sellerAccessToken = "")
        {
            var status = _orders.TryGetValue(externalOrderId, out var record)
                ? record.Status
                : MercadoPagoOrderStatus.Unknown;

            return Task.FromResult(new OrderStatusResult(externalOrderId, status));
        }

        public Task RefundAsync(
            string externalOrderId, Money? amount, CancellationToken cancellationToken, string sellerAccessToken = "")
        {
            // No real money motion, obviously — just records the order as refunded, same as every
            // other state transition here.
            SetStatus(externalOrderId, MercadoPagoOrderStatus.Refunded);
            return Task.CompletedTask;
        }

        public SimulatedOrderRecord? Get(string orderId) =>
            _orders.TryGetValue(orderId, out var record) ? record : null;

        public void SetStatus(string orderId, MercadoPagoOrderStatus status)
        {
            if (_orders.TryGetValue(orderId, out var existing))
            {
                _orders[orderId] = existing with { Status = status };
            }
        }
    }
}
