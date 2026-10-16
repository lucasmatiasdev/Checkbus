namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/public/trips/by-slug/{slug}</c> call. Mirrors
/// <see cref="PublicTripSeatMapOutcome"/>'s shape — anonymous endpoint, no Forbidden case.
/// </summary>
public abstract record ResolveTripSlugOutcome
{
    private ResolveTripSlugOutcome()
    {
    }

    /// <summary>200 OK — the slug resolved to this TripId.</summary>
    public sealed record Success(Guid TripId) : ResolveTripSlugOutcome;

    /// <summary>404 Not Found — no Trip exists with the given slug.</summary>
    public sealed record NotFound : ResolveTripSlugOutcome;

    /// <summary>Any transport-level failure, short and user-presentable.</summary>
    public sealed record TransportError(string Message) : ResolveTripSlugOutcome;
}
