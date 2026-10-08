namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Result of a successful CreatePublicReservationCommand — just enough for the public page's
    // inline success view (seat number, passenger name, price per seat) and the created ids,
    // deliberately NOT a full Reservation/Passenger projection (same minimal-exposure rationale
    // as PublicTripSeatMapDto). The dedicated, persistent/shareable confirmation page keyed by
    // these ids is explicitly R5's job, not built here.
    public class CreatePublicReservationResultDto
    {
        public required IReadOnlyList<CreatedReservationDto> Reservations { get; set; }
    }

    public class CreatedReservationDto
    {
        public required Guid ReservationId { get; set; }
        public required int SeatNumber { get; set; }
        public required string PassengerName { get; set; }
        public required string PassengerSurname { get; set; }
        public required decimal Price { get; set; }
    }
}
