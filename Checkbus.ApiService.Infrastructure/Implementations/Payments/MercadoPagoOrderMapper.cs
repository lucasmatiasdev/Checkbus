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

        public static string FormatAmount(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);

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

                }
            }

            return order.Transactions?.Payments?.FirstOrDefault()?.PaymentMethod?.RedirectUrl;
        }

        public static OrderStatusResult ToOrderStatusResult(Order order) =>
            new(order.Id, MapStatus(order));

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

        public static OrderRefundPaymentRequest BuildFullRefundRequest() => new();

        public static OrderRefundPaymentRequest BuildPartialRefundRequest(string paymentId, decimal amount) => new()
        {
            Transactions = new List<OrderRefundTransactionRequest>
            {
                new() { Id = paymentId, Amount = FormatAmount(amount) }
            }
        };
    }
}
