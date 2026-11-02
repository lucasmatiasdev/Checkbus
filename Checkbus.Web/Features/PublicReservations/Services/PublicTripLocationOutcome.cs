using Checkbus.Web.Features.PublicReservations.Contracts;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/public/trips/{id}/location</c> call
/// (odd/tasks/seguimiento-tiempo-real-demo.md T4). Unlike <see cref="PublicTripDetailOutcome"/>,
/// no <c>NotFound</c> case: that endpoint always answers 200 by design (see
/// <c>GetTripLocationQueryHandler</c>'s own doc comment), so the only split here is whether the
/// HTTP call itself succeeded.
/// </summary>
public abstract record PublicTripLocationOutcome
{
    private PublicTripLocationOutcome()
    {
    }

    /// <summary>200 OK — the location poll was fetched successfully (tracking may still be off;
    /// see <see cref="TripLocationResponse.LiveTrackingEnabled"/>).</summary>
    public sealed record Success(TripLocationResponse Location) : PublicTripLocationOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : PublicTripLocationOutcome;
}
