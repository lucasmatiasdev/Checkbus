namespace Checkbus.ApiService.Domain.Entities.Seats
{

    public class Seat
    {
        public Guid Id { get; set; }
        public required Guid VehicleId { get; set; }
        public required int Number { get; set; }
    }
}
