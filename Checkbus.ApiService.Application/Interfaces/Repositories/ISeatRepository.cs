using Checkbus.ApiService.Domain.Entities.Seats;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface ISeatRepository
    {
        // Lazily, idempotently backfills the Seat rows a Vehicle never got at registration
        // time (nothing in this codebase creates them yet — see
        // odd/tasks/reservas-publicas.md R3). Loads existing Seat.Number values for the
        // vehicle; if fewer than `capacity` exist, inserts the missing numbers
        // (1..capacity) in one SaveChangesAsync call. A no-op read when seats already exist,
        // so it is safe to call on every public seat-map request. Not safe against a
        // concurrent first-call race beyond the unique index on (VehicleId, Number) added in
        // R1 — a losing concurrent call surfaces the unique-constraint violation as a
        // transient 500, which is acceptable for this rare cold-start race (unrelated to the
        // actual seat-booking race R4 guards against with real Reservation rows).
        Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken);

        Task<IReadOnlyList<Seat>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken);
    }
}
