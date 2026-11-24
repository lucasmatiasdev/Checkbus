using System.Text;
using System.Text.Json;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers.Public
{

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

        [HttpPost]
        public async Task<IActionResult> Receive(
            [FromQuery(Name = "data.id")] string? dataId, CancellationToken cancellationToken)
        {
            var requestId = Request.Headers["x-request-id"].ToString();
            var signatureHeader = Request.Headers["x-signature"].ToString();

            if (!MercadoPagoWebhookSignatureValidator.IsValid(dataId, requestId, signatureHeader, _options.WebhookSecret))
            {

                _logger.LogWarning("Mercado Pago webhook signature validation failed.");
                return Unauthorized();
            }

            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                rawBody = await reader.ReadToEndAsync(cancellationToken);
            }

            var externalEventId = TryExtractTopLevelId(rawBody);
            if (string.IsNullOrEmpty(externalEventId) || string.IsNullOrEmpty(dataId))
            {

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
