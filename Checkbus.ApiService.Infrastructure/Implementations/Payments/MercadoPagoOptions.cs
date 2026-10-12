namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// Mercado Pago gateway configuration. Same permissive-binding posture as
    /// <c>EmailOptions</c>/<c>GoogleMapsOptions</c>: real sandbox credentials have not been
    /// provisioned yet (G1, a later Fase of odd/tasks/pagos-mercadopago.md) so a missing
    /// "MercadoPago" config section or blank values must not fail startup — only actually
    /// calling <c>MercadoPagoPaymentService</c> with no usable <see cref="AccessToken"/> fails,
    /// at call time, the same way an unconfigured <c>SmtpEmailService</c> only fails when it
    /// tries to connect.
    /// </summary>
    public class MercadoPagoOptions
    {
        public string AccessToken { get; set; } = string.Empty;

        // Validates the "x-signature"/"x-request-id" HMAC on incoming webhook notifications
        // (Fase D2) — not used by this Fase's outbound-only adapter, but bound here so both
        // live under one config section/options class.
        public string WebhookSecret { get; set; } = string.Empty;
    }
}
