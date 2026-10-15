namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// Config for <see cref="SimulatedPaymentService"/> (odd/tasks/pagos-mercadopago.md Fase J,
    /// J1) — the demo/academic-project workaround for I10 (Mercado Pago account KYC blocker).
    /// Same permissive-binding posture as <c>MercadoPagoOptions</c>/<c>PublicWebOptions</c>: a
    /// missing "Simulation" config section or a blank <see cref="ApiBaseUrl"/> must not fail
    /// startup — it is only actually needed once <c>MercadoPagoOptions.Simulated</c> is turned on
    /// and a checkout URL is built.
    /// </summary>
    public class SimulatedPaymentOptions
    {
        // The API's own externally-reachable base URL (e.g. the tunnel/host serving this app),
        // used to build the fake checkout_url SimulatedPaymentService.CreateOrderAsync returns
        // (it must point back at this same app's SimulatedCheckoutController, J2). Blank until
        // provisioned per-environment (appsettings.Development.json/user-secrets) — a blank value
        // simply yields a relative checkout_url rather than throwing.
        public string ApiBaseUrl { get; set; } = string.Empty;
    }
}
