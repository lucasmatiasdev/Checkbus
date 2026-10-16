using Checkbus.Web.Features.PublicReservations.Contracts;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/public/organizations/{orgSlug}/trips</c> call
/// (odd/tasks/viajes-disponibles-publico.md T2). Mirrors <see cref="ResolveTripSlugOutcome"/>'s
/// shape — anonymous endpoint, no Forbidden case.
/// </summary>
/// <remarks>
/// The API side always answers 200 OK with a (possibly empty) list, even for an unknown
/// orgSlug — see <c>GetPublicOrganizationTripsQueryHandler</c>'s doc comment for why. The
/// <see cref="NotFound"/> case is kept anyway for the same structural reason every other
/// anonymous-endpoint outcome in this folder keeps one: defensive consistency if the API's
/// posture ever changes, not because the current API response can produce it.
/// </remarks>
public abstract record GetPublicOrganizationTripsOutcome
{
    private GetPublicOrganizationTripsOutcome()
    {
    }

    /// <summary>200 OK — the (possibly empty) trip list was fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<PublicOrganizationTripResponse> Trips) : GetPublicOrganizationTripsOutcome;

    /// <summary>404 Not Found — not currently returned by the API (see remarks above).</summary>
    public sealed record NotFound : GetPublicOrganizationTripsOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : GetPublicOrganizationTripsOutcome;
}
