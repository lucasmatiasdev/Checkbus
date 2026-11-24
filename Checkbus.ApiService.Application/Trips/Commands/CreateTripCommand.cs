using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public class CreateTripCommand : IRequest<TripDto>
    {
        public required Guid VehicleId { get; set; }
        public required Guid DriverId { get; set; }
        public required Guid EventId { get; set; }
        public required DateTime DepartureDate { get; set; }
        public required DateTime ArrivalDate { get; set; }
        public required decimal Price { get; set; }

        public required IReadOnlyList<CreateTripStopCommand> Stops { get; set; }

    }

    public class CreateTripStopCommand
    {
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string PlaceId { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }

        public int StopDurationMinutes { get; set; }
        public bool IsBoardingPoint { get; set; }
    }
}
