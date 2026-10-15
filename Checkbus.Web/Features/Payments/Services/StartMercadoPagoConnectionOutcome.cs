namespace Checkbus.Web.Features.Payments.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/mercadopago-connections/start</c> call, covering every
/// realistic server response — same pattern as <c>MercadoPagoConnectionStatusOutcome</c>.
/// </summary>
public abstract record StartMercadoPagoConnectionOutcome
{
    private StartMercadoPagoConnectionOutcome()
    {
    }

    /// <summary>200 OK — the Mercado Pago-hosted authorization URL to navigate the browser to.</summary>
    public sealed record Success(string AuthorizationUrl) : StartMercadoPagoConnectionOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : StartMercadoPagoConnectionOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : StartMercadoPagoConnectionOutcome;
}
