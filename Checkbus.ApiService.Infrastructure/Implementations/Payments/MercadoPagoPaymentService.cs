using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Domain.Exceptions.Payments;
using Checkbus.ApiService.Domain.ValueObjects;
using MercadoPago.Client;
using MercadoPago.Client.Order;

namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// <see cref="IPaymentService"/> adapter over the Mercado Pago Orders API
    /// (<c>mercadopago-sdk</c> 2.12.1, <see cref="OrderClient"/>) — odd/tasks/pagos-mercadopago.md
    /// Fase B. All request/response mapping lives in <see cref="MercadoPagoOrderMapper"/>, kept
    /// separate so it is unit-testable without a live HTTP call.
    ///
    /// <para><b>Access-token mechanism (verified against the installed SDK, not assumed):</b>
    /// this class deliberately sets <see cref="RequestOptions.AccessToken"/> per call instead of
    /// the SDK's static <c>MercadoPago.Config.MercadoPagoConfig.AccessToken</c> global. The
    /// static global is process-wide mutable state — acceptable for a single hardcoded sandbox
    /// token in a throwaway script, but wrong for a DI-scoped service in an ASP.NET Core app (a
    /// concurrent request could race another request's token) and harder to unit test. Every
    /// <see cref="OrderClient"/> call below passes an explicit <see cref="RequestOptions"/>
    /// instead.</para>
    ///
    /// <para><b>Fase I (Marketplace/Split Payments):</b> that per-call <c>RequestOptions</c>
    /// value now comes from each <see cref="IPaymentService"/> method's own
    /// <c>sellerAccessToken</c> parameter (a specific Organization's connected seller token,
    /// resolved by <c>IMercadoPagoSellerTokenResolver</c>) instead of a <c>MercadoPagoOptions</c>
    /// field read internally — this class no longer takes <c>MercadoPagoOptions</c> as a
    /// dependency at all, since it had nothing left to use it for once the single global
    /// <c>AccessToken</c> stopped being this adapter's source of truth. This is exactly why Fase
    /// B built the per-call mechanism above instead of the SDK's static global in the first
    /// place: it made this change a matter of "pass a different string in", not a redesign.</para>
    ///
    /// <para><b>Idempotency:</b> every order-creation and refund call gets a fresh
    /// <see cref="Guid"/> in the <c>X-Idempotency-Key</c> header via
    /// <see cref="RequestOptions.CustomHeaders"/>, per Mercado Pago's own requirement for both
    /// operations (confirmed this session; reflecting <see cref="RequestOptions"/> on the
    /// installed package confirms <c>CustomHeaders</c> is the SDK's mechanism for custom
    /// headers).</para>
    ///
    /// <para><b>checkout_url caveat:</b> see <see cref="MercadoPagoOrderMapper.ExtractCheckoutUrl"/>'s
    /// doc comment — the installed SDK's <c>Order</c> resource does not expose a dedicated
    /// checkout-redirect URL the way the older Preference resource does; this is a real,
    /// unresolved product question for Fase C, not papered over here.</para>
    /// </summary>
    public class MercadoPagoPaymentService : IPaymentService
    {
        private readonly OrderClient _orderClient = new();

        public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken, string sellerAccessToken = "")
        {
            var sdkRequest = MercadoPagoOrderMapper.BuildCreateRequest(request);

            try
            {
                var order = await _orderClient.CreateAsync(sdkRequest, BuildRequestOptions(sellerAccessToken), cancellationToken);
                return MercadoPagoOrderMapper.ToCreateOrderResult(order);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The one call this Fase's spec calls out by name: "If the Mercado Pago call
                // itself fails, throw OrderCreationFailedException from CreateOrderAsync
                // specifically — don't swallow it." Fase C3 catches this type to clean up the
                // PendientePago batch an order-creation failure left behind.
                throw new OrderCreationFailedException($"Mercado Pago order creation failed: {ex.Message}");
            }
        }

        public async Task<OrderStatusResult> GetOrderAsync(string externalOrderId, CancellationToken cancellationToken, string sellerAccessToken = "")
        {
            try
            {
                var order = await _orderClient.GetAsync(externalOrderId, BuildRequestOptions(sellerAccessToken), cancellationToken);
                return MercadoPagoOrderMapper.ToOrderStatusResult(order);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // No dedicated Domain exception exists for "order lookup failed" (Fase A only
                // added OrderCreationFailedException for the create path, plus
                // PaymentNotFoundException/PaymentNotRefundableException for local repository
                // lookups/state checks) — adding a new one is outside this Fase's allowed edit
                // surface (Domain layer). Same posture SmtpEmailService already uses for its own
                // non-domain-exception-having failures: wrap in InvalidOperationException rather
                // than let an SDK-specific exception type leak out of this adapter.
                throw new InvalidOperationException($"Failed to fetch Mercado Pago order '{externalOrderId}'.", ex);
            }
        }

        public async Task RefundAsync(string externalOrderId, Money? amount, CancellationToken cancellationToken, string sellerAccessToken = "")
        {
            try
            {
                var refundRequest = amount is null
                    ? MercadoPagoOrderMapper.BuildFullRefundRequest()
                    : await BuildPartialRefundRequestAsync(externalOrderId, amount, sellerAccessToken, cancellationToken);

                await _orderClient.RefundAsync(externalOrderId, refundRequest, BuildRequestOptions(sellerAccessToken), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not InvalidOperationException)
            {
                // Same wrapping posture as GetOrderAsync above — no dedicated Domain exception
                // exists for a failed refund call either; Fase F2 is what decides how to react
                // to this (log + surface for manual follow-up, the Reservation stays cancelled
                // regardless).
                throw new InvalidOperationException($"Failed to refund Mercado Pago order '{externalOrderId}'.", ex);
            }
        }

        // A partial refund targets one specific payment attempt within the order (by its id),
        // which the order itself must be fetched to discover — see
        // MercadoPagoOrderMapper.BuildPartialRefundRequest's doc comment.
        private async Task<OrderRefundPaymentRequest> BuildPartialRefundRequestAsync(
            string externalOrderId, Money amount, string sellerAccessToken, CancellationToken cancellationToken)
        {
            var order = await _orderClient.GetAsync(externalOrderId, BuildRequestOptions(sellerAccessToken), cancellationToken);
            var paymentId = order.Transactions?.Payments?.FirstOrDefault()?.Id
                ?? throw new InvalidOperationException(
                    $"Cannot issue a partial refund for order '{externalOrderId}': no payment attempt was found on it.");

            return MercadoPagoOrderMapper.BuildPartialRefundRequest(paymentId, amount.Amount);
        }

        // Fase I (Marketplace/Split Payments): the access token used per call is the specific
        // Organization's own connected seller token passed in by the caller — there is no global
        // fallback token (MercadoPagoOptions.AccessToken was removed as dead code once this
        // became the only path). A blank sellerAccessToken (the three existing callers not yet
        // wired to IMercadoPagoSellerTokenResolver, per I6) results in RequestOptions.AccessToken = ""
        // exactly as before this Fase existed for those call sites — a real Mercado Pago call
        // with a blank token fails at the gateway (401), not silently, which is the correct
        // "not wired up yet" behavior until a later Fase does wire it in.
        private static RequestOptions BuildRequestOptions(string sellerAccessToken)
        {
            // RequestOptions.CustomHeaders is a get-only dictionary on the installed SDK (2.12.1)
            // — entries must be added into it, not replaced with a new Dictionary instance.
            var requestOptions = new RequestOptions { AccessToken = sellerAccessToken };
            requestOptions.CustomHeaders["X-Idempotency-Key"] = Guid.NewGuid().ToString();
            return requestOptions;
        }
    }
}
