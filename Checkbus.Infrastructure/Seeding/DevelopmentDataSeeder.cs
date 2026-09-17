using Checkbus.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Checkbus.Infrastructure.Seeding
{
    /// <summary>
    /// Development-only orchestrator: checks the idempotency guard, builds the fixed seed graph,
    /// and persists it in one call. Never invoked outside Development — the sole gate lives at the
    /// <c>Program.cs</c> call site (spec "Development-Only Gating").
    /// </summary>
    public sealed class DevelopmentDataSeeder
    {
        private readonly ISeedDataStore _store;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<DevelopmentDataSeeder> _logger;

        public DevelopmentDataSeeder(
            ISeedDataStore store,
            IPasswordHasher passwordHasher,
            ILogger<DevelopmentDataSeeder> logger)
        {
            _store = store;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<SeedOutcome> SeedAsync(CancellationToken cancellationToken = default)
        {
            if (await _store.HasAnyOrganizationAsync(cancellationToken))
            {
                _logger.LogInformation(
                    "Development seed skipped: at least one organization already exists.");
                return SeedOutcome.Skipped;
            }

            var graph = DevelopmentSeedData.Build(_passwordHasher);
            await _store.PersistAsync(graph, cancellationToken);

            _logger.LogInformation(
                "Development seed data created: {OrganizationCount} organizations, {UserCount} users.",
                graph.Organizations.Count,
                graph.Users.Count);

            return SeedOutcome.Seeded;
        }
    }
}
