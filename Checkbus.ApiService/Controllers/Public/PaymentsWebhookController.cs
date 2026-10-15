using System.Text;
using System.Text.Json;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers.Public
{
    // Fase D1 (odd/tasks/pagos-mercadopago.md) — Mercado Pago calling IN, not an authenticated
    // user: same [AllowAnonymous]/no-ICurrentUserService posture as PublicTripsController/
    // PublicReservationsController, for the same reason (there is no caller identity to check
    // here at all, let alone scope by). Deliberately NOT given the "public-reservations" rate-
    // limit policy those two controllers carry — that policy is sized for human browser traffic,
    // and Mercado Pago is documented to retry aggressively on anything but a fast 2xx; rate-
    // limiting its retries would actively work against this Fase's own correctness goal.
    //
    // Signature validation (D2) runs FIRST, before any body read/parse, DB access, or IMediator
    // dispatch — a request that fails it gets a bare 401 and nothing else happens. Per the
    // task's own explicit instruction: never log the secret, the provided signature, or the
    // computed HMAC, even on failure.
    [AllowAnonymous]
    [Route("api/public/payments/webhook")]
    [ApiController]
    public class PaymentsWebhookController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly MercadoPagoOptions _options;
        private readonly ILogger<PaymentsWebhookController> _logger;

        public PaymentsWebhookController(
            IMediator mediator, MercadoPagoOptions options, ILogger<PaymentsWebhookController> logger)
        {
            _mediator = mediator;
            _options = options;
            _logger = logger;
        }

        // data.id is sent by Mercado Pago on the webhook URL's own query string
        // (?data.id=...&type=order), confirmed via official docs — NOT just inside the JSON
        // body (see MercadoPagoWebhookSignatureValidator's doc comment for the full confirmed-
        // facts citation). [FromQuery(Name = "data.id")] binds that literal dotted key.
        [HttpPost]
        public async Task<IActionResult> Receive(
            [FromQuery(Name = "data.id")] string? dataId, CancellationToken cancellationToken)
        {
            var requestId = Request.Headers["x-request-id"].ToString();
            var signatureHeader = Request.Headers["x-signature"].ToString();

            if (!MercadoPagoWebhookSignatureValidator.IsValid(dataId, requestId, signatureHeader, _options.WebhookSecret))
            {
                // No secret/signature/HMAC value in this log line — see class doc comment.
                _logger.LogWarning("Mercado Pago webhook signature validation failed.");
                return Unauthorized();
            }

            // Read once, after signature validation (which needs only the query string + headers
            // above, never the body itself), before any model-binding/deserialization — reused
            // below to extract the notification's own top-level "id" (D3's dedupe key).
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                rawBody = await reader.ReadToEndAsync(cancellationToken);
            }

            var externalEventId = TryExtractTopLevelId(rawBody);
            if (string.IsNullOrEmpty(externalEventId) || string.IsNullOrEmpty(dataId))
            {
                // A signature-valid but otherwise malformed notification (missing id/data.id) —
                // nothing useful to process, but acknowledging with 200 anyway matches this
                // Fase's own rule: only a signature failure gets a non-2xx, so Mercado Pago does
                // not retry a notification it could never fix by itself.
                return Ok();
            }

            await _mediator.Send(
                new HandlePaymentWebhookCommand { ExternalEventId = externalEventId, ExternalOrderId = dataId },
                cancellationToken);

            return Ok();
        }

        private static string? TryExtractTopLevelId(string rawBody)
        {
            try
            {
                using var document = JsonDocument.Parse(rawBody);
                if (!document.RootElement.TryGetProperty("id", out var idElement))
                {
                    return null;
                }

                return idElement.ValueKind switch
                {
                    JsonValueKind.String => idElement.GetString(),
                    JsonValueKind.Number => idElement.GetRawText(),
                    _ => null
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
