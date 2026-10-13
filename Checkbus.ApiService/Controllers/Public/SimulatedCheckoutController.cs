using System.Net;
using System.Text;
using System.Text.Json;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers.Public
{
    // Fase J2 (odd/tasks/pagos-mercadopago.md) — the self-contained fake Mercado Pago checkout
    // page for the I10 KYC-blocker workaround. [AllowAnonymous]/no ICurrentUserService, same
    // posture as PaymentsWebhookController/PublicReservationsController: there is no buyer
    // identity to check here, and Mercado Pago's own real hosted checkout page has none either.
    //
    // Only reachable at all when MercadoPagoOptions.Simulated is true (that flag is what makes
    // Program.cs register ISimulatedOrderStore/SimulatedPaymentService in the first place — see
    // J3's wiring); with it false (the default), hitting this controller fails DI resolution of
    // ISimulatedOrderStore rather than silently doing anything, which is the correct "not enabled
    // in this environment" behavior for a demo-only surface.
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

        // Renders the fake checkout page itself — a plain ContentResult, no view engine (this
        // codebase has no server-rendered-view convention to match, per this Fase's own note).
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

            // This is what makes the simulation exercise the REAL signature validation, dedupe,
            // and HandlePaymentWebhookCommandHandler logic (Fase D) — not a shortcut around any
            // of it. A failure here (e.g. the app's own webhook endpoint unreachable) is logged
            // and swallowed rather than stranding the buyer on a 500 — this is a demo path, not a
            // real payment, so the worst case is a hold that never gets confirmed, same as Fase
            // E's existing lazy-expiry story already handles.
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

            // Built from the CURRENT request's own scheme+host, never a hardcoded config value —
            // so this works correctly behind whatever tunnel/host is actually serving this
            // request (per this Fase's own explicit instruction).
            var webhookUrl =
                $"{Request.Scheme}://{Request.Host}/api/public/payments/webhook?data.id={Uri.EscapeDataString(orderId)}";

            // Only a top-level "id" is strictly required by PaymentsWebhookController's own
            // TryExtractTopLevelId (Fase D1) — type/data.id are included too, for realism, but
            // aren't functionally required by the controller itself.
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
                <div style="background:#ffeb3b;color:#000;padding:1rem;font-weight:bold;">
                SIMULACIÓN &mdash; Proyecto acad&eacute;mico, no se procesa ning&uacute;n pago real
                </div>
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
