using Checkbus.Web.Features.Trips.Contracts;

namespace Checkbus.Web.Features.TripsReport.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Application.Trips.Queries.TripsReportDto</c>, the
/// response body shape for <c>GET /api/Trips/report</c>. Checkbus.Web does not reference the API
/// projects (pure BFF), so this DTO is duplicated here rather than shared. Reuses
/// <see cref="TripResponse"/> for the trip list — same denormalized shape as the existing trips
/// listing, no need for a second mirror.
/// </summary>
public sealed record TripsReportResponse
{
    public required IReadOnlyList<TripResponse> Trips { get; init; }
    public required TripsReportStatisticsResponse Statistics { get; init; }
}
