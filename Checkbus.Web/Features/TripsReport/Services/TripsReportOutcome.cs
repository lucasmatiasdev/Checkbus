using Checkbus.Web.Features.TripsReport.Contracts;

namespace Checkbus.Web.Features.TripsReport.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/Trips/report</c> call, covering every realistic server
/// response so callers (the Reportes Razor page) can pattern-match on the result without
/// inspecting HTTP status codes directly. Plain filtered read, same shape as
/// <see cref="Checkbus.Web.Features.Users.Services.UsersListOutcome"/> — no validation/conflict
/// case. The private constructor confines the hierarchy to the nested cases declared here.
/// </summary>
public abstract record TripsReportOutcome
{
    private TripsReportOutcome()
    {
    }

    /// <summary>200 OK — the filtered report was fetched successfully.</summary>
    public sealed record Success(TripsReportResponse Report) : TripsReportOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : TripsReportOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : TripsReportOutcome;
}
