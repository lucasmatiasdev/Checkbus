using Checkbus.Web.Features.Trips.Contracts;

namespace Checkbus.Web.Features.TripsReport.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Queries.TripsReportStatisticsDto</c>. Checkbus.Web
/// does not reference the API projects (pure BFF), so this DTO is duplicated here rather than
/// shared. <see cref="TripsByStatus"/> keys mirror <see cref="WebTripStatus"/>, matching the real
/// DTO's <c>Dictionary&lt;TripStatus, int&gt;</c> shape.
/// </summary>
public sealed record TripsReportStatisticsResponse
{
    public required int TotalTrips { get; init; }
    public required Dictionary<WebTripStatus, int> TripsByStatus { get; init; }
    public required int TotalPassengers { get; init; }
    public required decimal EstimatedRevenue { get; init; }
}
