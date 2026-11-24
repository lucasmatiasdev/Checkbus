namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class CreatePublicReservationResultDto
    {
        public required IReadOnlyList<CreatedReservationDto> Reservations { get; set; }
        public string? CheckoutUrl { get; set; }
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
