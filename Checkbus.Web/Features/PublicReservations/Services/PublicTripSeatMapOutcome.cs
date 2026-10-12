using Checkbus.Web.Features.PublicReservations.Contracts;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/public/trips/{id}/seatmap</c> call. Mirrors
/// <see cref="Checkbus.Web.Features.Trips.Services.TripOutcome"/>'s shape, minus a
/// Forbidden case — this endpoint is anonymous, so the API never returns 401/403 for it.
/// </summary>
public abstract record PublicTripSeatMapOutcome
{
    private PublicTripSeatMapOutcome()
    {
    }

    /// <summary>200 OK — the seat map was fetched successfully.</summary>
    public sealed record Success(PublicTripSeatMapResponse SeatMap) : PublicTripSeatMapOutcome;

    /// <summary>
    /// 404 Not Found — either no Trip exists with the given id, or it exists but is not
    /// Publicado. Both cases are indistinguishable by design (IDOR-safe).
    /// </summary>
    public sealed record NotFound : PublicTripSeatMapOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : PublicTripSeatMapOutcome;
}
