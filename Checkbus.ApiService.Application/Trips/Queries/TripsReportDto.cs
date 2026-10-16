using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class TripsReportDto
    {
        public required IReadOnlyList<TripDto> Trips { get; set; }
        public required TripsReportStatisticsDto Statistics { get; set; }
    }

    // Derived only from fields that already exist on Trip (Capacity, AvailableSeats,
    // Price, Status) — no new Trip columns for this first iteration of CU-07.
    public class TripsReportStatisticsDto
    {
        public int TotalTrips { get; set; }
        public Dictionary<TripStatus, int> TripsByStatus { get; set; } = [];
        public int TotalPassengers { get; set; }
        public decimal EstimatedRevenue { get; set; }
    }
}
