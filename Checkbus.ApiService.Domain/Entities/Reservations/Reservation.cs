using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Reservations
{

    public class Reservation
    {
        public Guid Id { get; set; }
        public required Guid TripId { get; set; }
        public required Guid SeatId { get; set; }
        public required Guid PassengerId { get; set; }
        public required decimal Price { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Confirmada;

        public Guid? PaymentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
