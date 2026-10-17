using Checkbus.ApiService.Domain.Entities.Tenancy;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IOrganizationRepository
    {
        // Added for RegisterOrganizationCommandHandler (odd/tasks/alta-organizacion-soporte.md
        // T1) — the interface had no write path at all before this feature; every prior
        // Organization row came from CheckbusDbSeeder writing the DbContext directly.
        Task AddAsync(Organization organization, CancellationToken cancellationToken);

        Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken);

        // Added for the public trip-detail surface (odd/tasks/detalle-reserva-publica.md T2) —
        // the anonymous visitor needs the organization's display Name, not its Slug.
        Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken);

        // Reverse of GetSlugByIdAsync — added for the public "viajes disponibles" listing
        // (odd/tasks/viajes-disponibles-publico.md T1), the first public URL to carry
        // Organization.Slug itself (every prior public surface resolved Trip.Slug instead). Same
        // anonymous, unauthenticated posture as ITripRepository.GetIdBySlugAsync: the slug carries
        // no sensitive info and an unknown slug is handled by the caller as an empty result, never
        // a distinguishable error.
        Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken);

        // Both added for RegisterOrganizationCommandHandler (odd/tasks/alta-organizacion-soporte.md
        // T1). Deliberately GLOBAL (no organization scope, unlike Trip.Slug which is unique only
        // per-organization) — a CUIT or Slug belongs to exactly one Organization system-wide.
        Task<bool> CuitExistsAsync(string cuit, CancellationToken cancellationToken);
        Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);
    }
}
