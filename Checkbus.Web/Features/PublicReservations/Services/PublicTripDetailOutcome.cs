using Checkbus.Web.Features.PublicReservations.Contracts;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/public/trips/{id}/details</c> call
/// (odd/tasks/detalle-reserva-publica.md T2). Mirrors <see cref="PublicTripSeatMapOutcome"/>'s
/// shape exactly — no Forbidden case, this endpoint is anonymous.
/// </summary>
public abstract record PublicTripDetailOutcome
{
    private PublicTripDetailOutcome()
    {
    }

    /// <summary>200 OK — the trip detail was fetched successfully.</summary>
    public sealed record Success(PublicTripDetailResponse Detail) : PublicTripDetailOutcome;

    /// <summary>
    /// 404 Not Found — either no Trip exists with the given id, or it exists but is not
    /// Publicado. Both cases are indistinguishable by design (IDOR-safe).
    /// </summary>
    public sealed record NotFound : PublicTripDetailOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : PublicTripDetailOutcome;
}
