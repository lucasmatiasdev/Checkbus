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
        // Fase I (Marketplace/Split Payments) makes this field unused for real order
        // creation/refund — every real IPaymentService call now needs a specific Organization's
        // own seller AccessToken (resolved via IMercadoPagoSellerTokenResolver), not one global
        // token. Left in place deliberately rather than deleted: this Fase is additive, and a
        // later cleanup pass can decide whether anything still legitimately needs a
        // Checkbus-wide fallback token (e.g. a non-tenant-scoped admin tool).
        public string AccessToken { get; set; } = string.Empty;

        // Validates the "x-signature"/"x-request-id" HMAC on incoming webhook notifications
        // (Fase D2) — not used by this Fase's outbound-only adapter, but bound here so both
        // live under one config section/options class.
        public string WebhookSecret { get; set; } = string.Empty;

        // Fase I: Checkbus's OWN Mercado Pago platform-app OAuth credentials (the "Checkbus" app
        // registered in Mercado Pago's developer dashboard), used to authorize/exchange/refresh
        // an ORGANIZATION's seller connection — distinct from any Organization's own seller
        // AccessToken above. Same permissive/blank-until-provisioned binding posture as every
        // other field in this options class: a real Marketplace-model app has not been
        // provisioned yet (see odd/tasks/pagos-mercadopago.md Fase I's own documented blocker),
        // so a missing "MercadoPago" config section or blank values here must not fail startup
        // — only an actual OAuth call with a blank ClientId/ClientSecret fails, at call time.
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        // The URL Mercado Pago redirects back to after an Organization approves the connection
        // (must match whatever redirect URL is configured on the Marketplace app itself).
        public string RedirectUri { get; set; } = string.Empty;

        // Fase J (odd/tasks/pagos-mercadopago.md) — the I10 KYC-blocker workaround: when true,
        // Program.cs registers SimulatedPaymentService instead of MercadoPagoPaymentService as
        // IPaymentService. Defaults to false (same "safe when unconfigured" posture as every
        // other field here) — only an explicit "MercadoPago:Simulated": true in
        // appsettings.Development.json/user-secrets turns the demo path on; it must never be
        // true by default in appsettings.json.
        public bool Simulated { get; set; } = false;
    }
}
