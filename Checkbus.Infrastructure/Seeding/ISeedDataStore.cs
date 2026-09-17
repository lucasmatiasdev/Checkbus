using Checkbus.Domain.Entities.Authentication;
using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;

namespace Checkbus.Infrastructure.Seeding
{
    /// <summary>Outcome of a single <see cref="DevelopmentDataSeeder.SeedAsync"/> call.</summary>
    public enum SeedOutcome
    {
        Seeded,
        Skipped
    }

    /// <summary>
    /// Full development seed graph, ready to be persisted in one <c>SaveChangesAsync</c> call.
    /// </summary>
    public sealed record SeedGraph(
        IReadOnlyList<Role> Roles,
        IReadOnlyList<Profile> Profiles,
        IReadOnlyList<Organization> Organizations,
        IReadOnlyList<User> Users);

    /// <summary>
    /// Port for the Development-only seed persistence boundary. Kept separate from
    /// <see cref="DevelopmentDataSeeder"/> so the guard-then-insert orchestration can be unit
    /// tested against a substitute, without a real Postgres connection.
    /// </summary>
    public interface ISeedDataStore
    {
        Task<bool> HasAnyOrganizationAsync(CancellationToken cancellationToken = default);

        Task PersistAsync(SeedGraph graph, CancellationToken cancellationToken = default);
    }
}
