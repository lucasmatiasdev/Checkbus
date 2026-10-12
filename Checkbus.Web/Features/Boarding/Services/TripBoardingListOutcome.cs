using Checkbus.Web.Features.Boarding.Contracts;

namespace Checkbus.Web.Features.Boarding.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/boarding/{tripId}</c> call.
/// </summary>
public abstract record TripBoardingListOutcome
{
    private TripBoardingListOutcome()
    {
    }

    /// <summary>200 OK — the boarding list was fetched successfully.</summary>
    public sealed record Success(TripBoardingListResponse List) : TripBoardingListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : TripBoardingListOutcome;

    /// <summary>404 Not Found — the Trip doesn't exist, or doesn't belong to the caller.</summary>
    public sealed record NotFound : TripBoardingListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : TripBoardingListOutcome;
}
