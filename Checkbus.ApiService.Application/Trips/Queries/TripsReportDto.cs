using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class TripsReportDto
    {
        public required IReadOnlyList<TripDto> Trips { get; set; }
        public required TripsReportStatisticsDto Statistics { get; set; }
    }

    public class TripsReportStatisticsDto
    {
        public int TotalTrips { get; set; }
        public Dictionary<TripStatus, int> TripsByStatus { get; set; } = [];
        public int TotalPassengers { get; set; }
        public decimal EstimatedRevenue { get; set; }
    }
}
