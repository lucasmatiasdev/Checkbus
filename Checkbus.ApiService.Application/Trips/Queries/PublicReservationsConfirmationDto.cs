namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class PublicReservationsConfirmationDto
    {
        public required IReadOnlyList<ReservationConfirmationDto> Reservations { get; set; }
    }

    public class ReservationConfirmationDto
    {
        public required Guid ReservationId { get; set; }
        public int SeatNumber { get; set; }
        public required string PassengerName { get; set; }
        public required string PassengerSurname { get; set; }
        public Checkbus.ApiService.Domain.Enums.DocumentType PassengerDocumentType { get; set; }
        public required string PassengerDocumentNumber { get; set; }
        public decimal Price { get; set; }

        public required string Status { get; set; }

        public string? EventName { get; set; }
        public DateTime DepartureDate { get; set; }

        public required string TripStatus { get; set; }

        public Guid TripId { get; set; }
        public string? DriverName { get; set; }
        public string? DriverSurname { get; set; }
        public string? VehiclePatent { get; set; }
        public TimeSpan? EstimatedDuration { get; set; }
        public decimal? DistanceKm { get; set; }
        public DateTime? ActualArrivalDate { get; set; }
    }
}
