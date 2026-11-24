using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.BoardingRecords
{

    public class BoardingRecord
    {
        public Guid Id { get; set; }
        public required Guid ReservationId { get; set; }
        public BoardingStatus Status { get; set; } = BoardingStatus.Pendiente;
        public ValidationMethod? Method { get; set; }
        public DateTime? ValidatedAt { get; set; }
    }
}
