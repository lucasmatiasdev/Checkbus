namespace Checkbus.Web.Features.Payments.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/mercadopago-connections/status</c> call, covering every
/// realistic server response — same pattern as <c>UsersListOutcome</c>. A plain read with no
/// validation/conflict case, so only success, an auth failure, and a transport failure are
/// realistic.
/// </summary>
public abstract record MercadoPagoConnectionStatusOutcome
{
    private MercadoPagoConnectionStatusOutcome()
    {
    }

    /// <summary>200 OK.</summary>
    public sealed record Success(bool Connected, string? SellerUserId) : MercadoPagoConnectionStatusOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : MercadoPagoConnectionStatusOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : MercadoPagoConnectionStatusOutcome;
}
