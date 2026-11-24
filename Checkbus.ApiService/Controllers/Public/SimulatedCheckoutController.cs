using System.Net;
using System.Text;
using System.Text.Json;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers.Public
{

    [AllowAnonymous]
    [Route("api/public/payments/simulate")]
    [ApiController]
    public class SimulatedCheckoutController : ControllerBase
    {
        private readonly ISimulatedOrderStore _orderStore;
        private readonly MercadoPagoOptions _mercadoPagoOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SimulatedCheckoutController> _logger;

        public SimulatedCheckoutController(
            ISimulatedOrderStore orderStore,
            MercadoPagoOptions mercadoPagoOptions,
            IHttpClientFactory httpClientFactory,
            ILogger<SimulatedCheckoutController> logger)
        {
            _orderStore = orderStore;
            _mercadoPagoOptions = mercadoPagoOptions;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet("{orderId}")]
        public IActionResult Show(string orderId)
        {
            var record = _orderStore.Get(orderId);
            return record is null
                ? Content(NotFoundHtml(orderId), "text/html")
                : Content(CheckoutHtml(record), "text/html");
        }

        [HttpPost("{orderId}/approve")]
        public Task<IActionResult> Approve(string orderId, CancellationToken cancellationToken) =>
            ResolveAsync(orderId, MercadoPagoOrderStatus.Approved, cancellationToken);

        [HttpPost("{orderId}/reject")]
        public Task<IActionResult> Reject(string orderId, CancellationToken cancellationToken) =>
            ResolveAsync(orderId, MercadoPagoOrderStatus.Rejected, cancellationToken);

        private async Task<IActionResult> ResolveAsync(
            string orderId, MercadoPagoOrderStatus status, CancellationToken cancellationToken)
        {
            var record = _orderStore.Get(orderId);
            if (record is null)
            {
                return Content(NotFoundHtml(orderId), "text/html");
            }

            _orderStore.SetStatus(orderId, status);

            await PostSimulatedWebhookAsync(orderId, cancellationToken);

            var redirectUrl = status == MercadoPagoOrderStatus.Approved ? record.SuccessUrl : record.FailureUrl;
            return !string.IsNullOrWhiteSpace(redirectUrl)
                ? Redirect(redirectUrl)
                : Content(ThankYouHtml(status), "text/html");
        }

        private async Task PostSimulatedWebhookAsync(string orderId, CancellationToken cancellationToken)
        {
            var requestId = Guid.NewGuid().ToString();
            var signatureHeader = SimulatedWebhookSigner.BuildSignatureHeader(
                orderId, requestId, _mercadoPagoOptions.WebhookSecret);

            var webhookUrl =
                $"{Request.Scheme}://{Request.Host}/api/public/payments/webhook?data.id={Uri.EscapeDataString(orderId)}";

            var body = JsonSerializer.Serialize(new
            {
                id = Guid.NewGuid().ToString(),
                type = "order",
                data = new { id = orderId }
            });

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("x-request-id", requestId);
            httpRequest.Headers.Add("x-signature", signatureHeader);

            try
            {
                var client = _httpClientFactory.CreateClient("SimulatedWebhook");
                await client.SendAsync(httpRequest, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex, "Simulated webhook POST to the payments webhook endpoint failed for order {OrderId}.", orderId);
            }
        }

        private static string NotFoundHtml(string orderId) => $"""
            <html>
            <body>
            <h1>Orden no encontrada</h1>
            <p>{WebUtility.HtmlEncode(orderId)}</p>
            </body>
            </html>
            """;

        private static string CheckoutHtml(SimulatedOrderRecord record)
        {
            var escapedOrderId = Uri.EscapeDataString(record.OrderId);
            return $"""
                <html>
                <body>
                <h1>Checkout simulado</h1>
                <p>Monto: {WebUtility.HtmlEncode($"{record.TotalAmount.Amount} {record.TotalAmount.Currency}")}</p>
                <form method="post" action="/api/public/payments/simulate/{escapedOrderId}/approve">
                <button type="submit">Aprobar pago</button>
                </form>
                <form method="post" action="/api/public/payments/simulate/{escapedOrderId}/reject">
                <button type="submit">Rechazar pago</button>
                </form>
                </body>
                </html>
                """;
        }

        private static string ThankYouHtml(MercadoPagoOrderStatus status) => $"""
            <html>
            <body>
            <h1>Gracias</h1>
            <p>Estado: {WebUtility.HtmlEncode(status.ToString())}</p>
            </body>
            </html>
            """;
    }
}
