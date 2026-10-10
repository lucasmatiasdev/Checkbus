using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Response shape for the Chofer-facing boarding screen (odd/tasks/reservas-publicas.md
    // Fase 3 / A3). Deliberately excludes Passenger.Email/Passenger.Phone — same exclusion
    // discipline as PublicReservationsConfirmationDto, even though this surface requires an
    // authenticated Chofer rather than being anonymous: there is no reason to expose contact
    // data here either, the boarding screen only ever needs identity + seat + status.
    public class TripBoardingListDto
    {
        public required IReadOnlyList<BoardingPassengerDto> Passengers { get; set; }
    }

    public class BoardingPassengerDto
    {
        public required Guid ReservationId { get; set; }
        public int SeatNumber { get; set; }
        public required string PassengerName { get; set; }
        public required string PassengerSurname { get; set; }
        public DocumentType PassengerDocumentType { get; set; }
        public required string PassengerDocumentNumber { get; set; }
        public BoardingStatus Status { get; set; }
    }
}
