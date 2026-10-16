namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Response shape for the public confirmation page (odd/tasks/reservas-publicas.md R5).
    // Deliberately excludes Passenger.Email/Passenger.Phone — this page is reachable by
    // anyone holding a Reservation.Id, so it must never expose contact data the visitor
    // reading it didn't already provide for themselves (see
    // GetPublicReservationsConfirmationQueryHandlerTests for the assertion that these fields
    // are absent from the DTO's own shape, not just unset).
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

        // Exposed as a plain string (e.g. "Confirmada") rather than the ReservationStatus
        // enum itself: System.Text.Json serializes enums as numbers by default, and adding a
        // numeric Web-side mirror + reflection parity test (the TripStatus/WebTripStatus
        // pattern) is unwarranted infra for a value this page only displays, never branches
        // on — Fase 1 only has one Status value anyway.
        public required string Status { get; set; }

        // Trip-level display fields — same shape TripDetailDto already exposes (EventName,
        // DepartureDate), no new Trip concept invented here.
        public string? EventName { get; set; }
        public DateTime DepartureDate { get; set; }

        // CU-11 replacement ("Consultar estado y progreso del viaje", no GPS hardware): unlike
        // PublicTripDetailDto, Driver/Vehicle identity IS safe to expose here — holding a real
        // Reservation.Id already proves a confirmed booking on this trip, not just anonymous
        // browsing. EstimatedDuration/DistanceKm are the already-computed Route values from trip
        // creation (Google Directions), never recalculated live here.
        public required string TripStatus { get; set; }
        public string? DriverName { get; set; }
        public string? DriverSurname { get; set; }
        public string? VehiclePatent { get; set; }
        public TimeSpan? EstimatedDuration { get; set; }
        public decimal? DistanceKm { get; set; }
        public DateTime? ActualArrivalDate { get; set; }
    }
}
