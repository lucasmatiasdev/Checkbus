using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Trips.Queries
{

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
