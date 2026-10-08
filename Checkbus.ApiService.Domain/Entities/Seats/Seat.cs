namespace Checkbus.ApiService.Domain.Entities.Seats
{
    // A physical seat on a Vehicle. FK-only, no navigation property back to Vehicle — same
    // pattern as VehicleDocument -> Vehicle. One row per (Vehicle, Number) pair, enforced by
    // a unique index on (VehicleId, Number) in CheckbusDbContext. No per-vehicle custom
    // layout is stored: the public page renders a standard 2+2-with-aisle grid computed from
    // Vehicle.Capacity, Number just identifies one slot in that grid (1..Capacity).
    public class Seat
    {
        public Guid Id { get; set; }
        public required Guid VehicleId { get; set; }
        public required int Number { get; set; }
    }
}
