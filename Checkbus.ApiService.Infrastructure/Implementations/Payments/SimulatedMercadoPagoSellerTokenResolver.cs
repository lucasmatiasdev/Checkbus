using Checkbus.ApiService.Application.Interfaces.Payments;

namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// <see cref="IMercadoPagoSellerTokenResolver"/> stand-in for Fase J's simulated payment path
    /// (odd/tasks/pagos-mercadopago.md Fase J, the I10 KYC-blocker workaround). The real
    /// <see cref="MercadoPagoSellerTokenResolver"/> throws
    /// <see cref="Checkbus.ApiService.Domain.Exceptions.Payments.OrganizationNotConnectedToMercadoPagoException"/>
    /// when an Organization has no <c>MercadoPagoConnection</c> row — which, for the duration of
    /// I10's KYC blocker, is every Organization, since the OAuth connect flow this resolver
    /// depends on is exactly what cannot complete. Wiring the real resolver in alongside
    /// <see cref="SimulatedPaymentService"/> would throw before <c>CreateOrderAsync</c> is ever
    /// reached, defeating the whole simulation. This resolver always returns a constant dummy
    /// token instead — safe because <see cref="SimulatedPaymentService"/> ignores its
    /// <c>sellerAccessToken</c> parameter entirely (its in-memory store has no concept of a real
    /// seller account to charge).
    /// </summary>
    public class SimulatedMercadoPagoSellerTokenResolver : IMercadoPagoSellerTokenResolver
    {
        private const string DummyToken = "SIMULATED-SELLER-TOKEN";

        public Task<string> ResolveAccessTokenAsync(Guid organizationId, CancellationToken cancellationToken) =>
            Task.FromResult(DummyToken);
    }
}
