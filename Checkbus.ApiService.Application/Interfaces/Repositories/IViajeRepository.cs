using Checkbus.ApiService.Domain.Entities.Rutas;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IViajeRepository
    {
        // Viaje, its Ruta and all Stops are added to the same ChangeTracker and saved in
        // one SaveChangesAsync call, so they are persisted atomically as a single
        // aggregate — same shape as IVehicleDiagnosticRepository.AddAsync(diagnostic, components).
        Task AddAsync(Viaje viaje, Ruta ruta, IEnumerable<Stop> stops, CancellationToken cancellationToken);
        Task<Viaje?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<Viaje>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);

        // Both overlap queries exist for R5's habilitación checks: an existing Viaje
        // overlaps the given range when existing.FechaSalida <= fechaLlegada &&
        // existing.FechaLlegada >= fechaSalida (standard interval-overlap condition).
        Task<IReadOnlyList<Viaje>> GetOverlappingByVehicleIdAsync(
            Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken);
        Task<IReadOnlyList<Viaje>> GetOverlappingByChoferIdAsync(
            Guid choferId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken);

        // Added in R5 so GetViajeQueryHandler can render a Viaje's route/stops detail — R1 only
        // needed to persist the aggregate, never to read Ruta/Stops back.
        Task<Ruta?> GetRutaByViajeIdAsync(Guid viajeId, CancellationToken cancellationToken);
        Task<IReadOnlyList<Stop>> GetStopsByRutaIdAsync(Guid rutaId, CancellationToken cancellationToken);
    }
}
