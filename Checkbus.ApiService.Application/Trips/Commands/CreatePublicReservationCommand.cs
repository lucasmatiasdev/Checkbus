using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // All-or-nothing batch reservation create for the public, unauthenticated reservation page
    // (odd/tasks/reservas-publicas.md R4). One (SeatId, passenger data) pair per selected seat —
    // a family of 3 booking together sends one command with 3 entries, not 3 separate requests.
    // Kept Trip-rooted under Trips/Commands rather than a new PublicReservations folder on the
    // API side, same foldering choice R3 made for GetPublicTripSeatMapQuery.
    public class CreatePublicReservationCommand : IRequest<CreatePublicReservationResultDto>
    {
        public required Guid TripId { get; set; }
        public required IReadOnlyList<PassengerSeatRequest> Passengers { get; set; }
    }

    // One passenger's own data for one selected Seat. DocumentType reuses the existing enum
    // (DNI/Pasaporte, same one Passenger uses) — nothing passenger-specific is invented here.
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
