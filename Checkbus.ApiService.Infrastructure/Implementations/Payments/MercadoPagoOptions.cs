namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// Mercado Pago gateway configuration. Same permissive-binding posture as
    /// <c>EmailOptions</c>/<c>GoogleMapsOptions</c>: real sandbox credentials have not been
    /// provisioned yet (G1, a later Fase of odd/tasks/pagos-mercadopago.md) so a missing
    /// "MercadoPago" config section or blank values must not fail startup — only actually
    /// calling <c>MercadoPagoPaymentService</c> with no usable seller token fails, at call time,
    /// the same way an unconfigured <c>SmtpEmailService</c> only fails when it tries to connect.
    /// </summary>
    public class MercadoPagoOptions
    {

        public string WebhookSecret { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;

        public bool Simulated { get; set; } = false;
    }
}
