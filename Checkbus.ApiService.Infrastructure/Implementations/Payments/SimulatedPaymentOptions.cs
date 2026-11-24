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

        public string ApiBaseUrl { get; set; } = string.Empty;
    }
}
