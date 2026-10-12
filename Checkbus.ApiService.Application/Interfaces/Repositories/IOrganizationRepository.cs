namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IOrganizationRepository
    {
        Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken);

        // Added for the public trip-detail surface (odd/tasks/detalle-reserva-publica.md T2) —
        // the anonymous visitor needs the organization's display Name, not its Slug.
        Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken);
    }
}
