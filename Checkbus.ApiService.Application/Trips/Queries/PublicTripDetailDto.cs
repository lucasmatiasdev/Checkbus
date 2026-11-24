using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class PublicTripDetailDto
    {
        public required Guid TripId { get; set; }
        public required string OrganizationName { get; set; }
        public required Guid EventId { get; set; }
        public required string EventName { get; set; }

        public bool EventHasImage { get; set; }

        public decimal Price { get; set; }

        public int AvailableSeats { get; set; }

        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }

        public required IReadOnlyList<StopDto> Stops { get; set; }

        public class StopDto
        {
            public int Order { get; set; }
            public StopType Type { get; set; }
            public required string Name { get; set; }
            public required string Address { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }

            public DateTime? DepartureTime { get; set; }
        }
    }
}
