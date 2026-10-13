using System.Globalization;
using System.Text.Json;
using Checkbus.ApiService.Application.Interfaces.Payments;
using MercadoPago.Client.Order;
using MercadoPago.Resource.Order;

namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// Pure mapping between our <see cref="IPaymentService"/> DTOs and the mercadopago-sdk
    /// 2.12.1 Orders API client types (<c>MercadoPago.Client.Order</c>/
    /// <c>MercadoPago.Resource.Order</c>). Pulled out of <see cref="MercadoPagoPaymentService"/>
    /// specifically so this logic is unit-testable with plain constructed SDK objects — no HTTP
    /// call, no live sandbox credentials needed (odd/tasks/pagos-mercadopago.md Fase B
    /// testability note).
    /// </summary>
    public static class MercadoPagoOrderMapper
    {
        // The Orders API's wire format represents every amount as a decimal-formatted string
        // ("150.00"), not a number — confirmed by reflecting the installed SDK's
        // OrderCreateRequest.TotalAmount/OrderPaymentRequest.Amount/
        // OrderRefundTransactionRequest.Amount, which are all typed string.
        public static string FormatAmount(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);

        // Builds a type: "online" order for Checkout Pro. Corrected after a live sandbox 400
        // ("$.transactions.payments[0]' - missing properties: 'payment_method'"): a
        // `transactions.payments[...]` entry is NOT part of Checkout Pro's order-creation
        // schema at all — that shape belongs to a different (automatic) processing flow where
        // the payment method is specified upfront. Checkout Pro uses `processing_mode: "manual"`
        // with no `transactions` object; the buyer picks everything on Mercado Pago's own
        // hosted page after redirect, confirmed against the official "Create order" API
        // reference (checkout-pro-orders/create-order).
        public static OrderCreateRequest BuildCreateRequest(CreateOrderRequest request)
        {
            var amount = FormatAmount(request.TotalAmount.Amount);

            return new OrderCreateRequest
            {
                Type = "online",
                ProcessingMode = "manual",
                TotalAmount = amount,
                ExternalReference = request.ExternalReference,
                Payer = request.PayerEmail is null ? null : new OrderPayerRequest { Email = request.PayerEmail },
                Config = BuildConfig(request)
            };
        }

        // C5: back_urls for the Orders API's type: "online" order live nested under
        // config.online (confirmed against Mercado Pago's own docs). Reflected the installed
        // mercadopago-sdk 2.12.1 assembly directly (MercadoPago.Client.Order.
        // OrderOnlineConfigRequest in MercadoPago.dll) to get the exact shape instead of
        // assuming it: SuccessUrl/FailureUrl/PendingUrl/CallbackUrl/AutoReturnUrl are all plain
        // string properties — there is NO "AutoReturn" approved/all string switch in this SDK
        // version, only AutoReturnUrl (itself a URL), so that field is intentionally left unset
        // here. Never send Mercado Pago an empty-string URL: Config/Config.Online is left null
        // entirely when none of the three back-URLs are set, and only the actually-populated
        // ones are assigned when at least one is.
        private static OrderConfigRequest? BuildConfig(CreateOrderRequest request)
        {
            var hasAnyBackUrl =
                !string.IsNullOrEmpty(request.SuccessUrl) ||
                !string.IsNullOrEmpty(request.FailureUrl) ||
                !string.IsNullOrEmpty(request.PendingUrl);

            if (!hasAnyBackUrl)
            {
                return null;
            }

            return new OrderConfigRequest
            {
                Online = new OrderOnlineConfigRequest
                {
                    SuccessUrl = string.IsNullOrEmpty(request.SuccessUrl) ? null : request.SuccessUrl,
                    FailureUrl = string.IsNullOrEmpty(request.FailureUrl) ? null : request.FailureUrl,
                    PendingUrl = string.IsNullOrEmpty(request.PendingUrl) ? null : request.PendingUrl
                }
            };
        }

        public static CreateOrderResult ToCreateOrderResult(Order order) =>
            new(order.Id, ExtractCheckoutUrl(order));

        // Deviation worth flagging (see MercadoPagoPaymentService's own doc comment): the
        // installed SDK's Order resource does not expose a dedicated "checkout_url"/init_point
        // property the way the older Preference resource does. Two fallbacks, in order:
        //   1. The raw, unmapped JSON response body — forward-compatible if Mercado Pago's
        //      Orders API actually sends a checkout_url the SDK's POCO just doesn't map yet.
        //   2. The first payment attempt's PaymentMethod.RedirectUrl — populated for
        //      redirect-based payment methods (e.g. Pix/boleto) once a payment attempt exists
        //      on the order; null for a freshly created order with no payment attempt.
        // Both can legitimately be null; CreateOrderResult.CheckoutUrl is nullable for exactly
        // this reason, and Fase C (the real caller) must decide what a null value means for its
        // redirect flow — not resolved in this Fase.
        public static string? ExtractCheckoutUrl(Order order)
        {
            var rawContent = order.ApiResponse?.Content;
            if (!string.IsNullOrEmpty(rawContent))
            {
                try
                {
                    using var document = JsonDocument.Parse(rawContent);
                    if (document.RootElement.TryGetProperty("checkout_url", out var checkoutUrlElement) &&
                        checkoutUrlElement.ValueKind == JsonValueKind.String)
                    {
                        return checkoutUrlElement.GetString();
                    }
                }
                catch (JsonException)
                {
                    // Malformed/absent raw content (e.g. a hand-built Order in a unit test with
                    // no ApiResponse set) — fall through to the PaymentMethod fallback below.
                }
            }

            return order.Transactions?.Payments?.FirstOrDefault()?.PaymentMethod?.RedirectUrl;
        }

        public static OrderStatusResult ToOrderStatusResult(Order order) =>
            new(order.Id, MapStatus(order));

        // Prefers the first payment attempt's own status (the real settlement outcome) over
        // the order-level status, falling back to the order-level status only when no payment
        // attempt exists yet (e.g. an order created but not yet paid, where Order.Status is
        // typically "created"). Vocabulary covers both the Orders API's and the legacy Payments
        // API's known status strings, since the two share the same underlying processor.
        public static MercadoPagoOrderStatus MapStatus(Order order)
        {
            var rawStatus = order.Transactions?.Payments?.FirstOrDefault()?.Status ?? order.Status;

            return rawStatus?.ToLowerInvariant() switch
            {
                "approved" or "accredited" => MercadoPagoOrderStatus.Approved,
                "pending" or "in_process" or "in_mediation" or "created" or "processed" => MercadoPagoOrderStatus.Pending,
                "rejected" or "cancelled" or "canceled" => MercadoPagoOrderStatus.Rejected,
                "refunded" or "charged_back" => MercadoPagoOrderStatus.Refunded,
                _ => MercadoPagoOrderStatus.Unknown
            };
        }

        // Empty body = total refund — Mercado Pago's own convention (confirmed this session),
        // mirrored by IPaymentService.RefundAsync's own doc comment.
        public static OrderRefundPaymentRequest BuildFullRefundRequest() => new();

        // A partial refund targets one specific payment attempt within the order (by its id),
        // not the order as a whole — the SDK's OrderRefundTransactionRequest models it that
        // way, so MercadoPagoPaymentService.RefundAsync must resolve that payment id (via
        // GetAsync) before calling this.
        public static OrderRefundPaymentRequest BuildPartialRefundRequest(string paymentId, decimal amount) => new()
        {
            Transactions = new List<OrderRefundTransactionRequest>
            {
                new() { Id = paymentId, Amount = FormatAmount(amount) }
            }
        };
    }
}
