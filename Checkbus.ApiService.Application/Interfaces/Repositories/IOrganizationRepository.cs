namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IOrganizationRepository
    {
        Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken);
    }
}
