namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IOrganizationRepository
    {
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
    }
}
