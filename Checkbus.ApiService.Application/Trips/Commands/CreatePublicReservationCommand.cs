using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class CreatePublicReservationCommand : IRequest<CreatePublicReservationResultDto>
    {
        public required Guid TripId { get; set; }
        public required IReadOnlyList<PassengerSeatRequest> Passengers { get; set; }
    }

    public class PassengerSeatRequest
    {
        public required Guid SeatId { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required DocumentType DocumentType { get; set; }
        public required string DocumentNumber { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
    }
}
