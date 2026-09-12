using Checkbus.Domain.Entities.Authentication.Authorization;

namespace Checkbus.Application.Abstractions
{
    public interface IProfileRepository
    {
        // MUST Include(p => p.Organization) — Profile's organization FK is a shadow property (D5);
        // callers rely on the navigation being loaded to perform the tenancy check.
        Task<Profile?> FindByIdAsync(int id, CancellationToken ct = default);
    }
}
