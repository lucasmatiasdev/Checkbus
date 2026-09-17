using Checkbus.Application.Abstractions;
using Checkbus.Infrastructure.Security;
using Checkbus.Infrastructure.Seeding;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Checkbus.UnitTests.Infrastructure.Seeding
{
    /// <summary>
    /// Covers the guard-then-insert orchestration (spec "Idempotency Guard", "Subsequent run",
    /// "First run on an empty database") and the seed graph shape (spec "Seed graph completeness",
    /// "Credential Hashing and Login Usability") against a substitute <see cref="ISeedDataStore"/>,
    /// with no real Postgres connection.
    /// </summary>
    public class DevelopmentDataSeederTests
    {
        private readonly ISeedDataStore _store = Substitute.For<ISeedDataStore>();
        private readonly IPasswordHasher _passwordHasher = new IdentityPasswordHasher();
        private readonly ILogger<DevelopmentDataSeeder> _logger = Substitute.For<ILogger<DevelopmentDataSeeder>>();

        private DevelopmentDataSeeder CreateSut() => new(_store, _passwordHasher, _logger);

        [Fact]
        public async Task SeedAsync_OrganizationsAlreadyExist_SkipsAndDoesNotPersist()
        {
            _store.HasAnyOrganizationAsync(Arg.Any<CancellationToken>()).Returns(true);

            var sut = CreateSut();
            var outcome = await sut.SeedAsync();

            Assert.Equal(SeedOutcome.Skipped, outcome);
            await _store.DidNotReceive().PersistAsync(Arg.Any<SeedGraph>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SeedAsync_NoOrganizationsExist_PersistsGraphAndReturnsSeeded()
        {
            _store.HasAnyOrganizationAsync(Arg.Any<CancellationToken>()).Returns(false);

            var sut = CreateSut();
            var outcome = await sut.SeedAsync();

            Assert.Equal(SeedOutcome.Seeded, outcome);
            await _store.Received(1).PersistAsync(Arg.Any<SeedGraph>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SeedAsync_NoOrganizationsExist_BuildsGraphMatchingSpecShape()
        {
            _store.HasAnyOrganizationAsync(Arg.Any<CancellationToken>()).Returns(false);
            SeedGraph? capturedGraph = null;
            _store.PersistAsync(Arg.Do<SeedGraph>(graph => capturedGraph = graph), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var sut = CreateSut();
            await sut.SeedAsync();

            Assert.NotNull(capturedGraph);
            var graph = capturedGraph!;

            Assert.Equal(2, graph.Organizations.Count);
            Assert.Contains(graph.Profiles, p => p.OrganizationId == null);
            Assert.NotEmpty(graph.Users);
            Assert.All(graph.Users, u => Assert.False(u.MustChangePassword));
            Assert.Contains(graph.Users, u => !u.IsActive);
            Assert.All(
                graph.Users,
                u => Assert.True(_passwordHasher.Verify(u.PasswordHash, DevelopmentSeedData.SeedPassword)));
        }
    }
}
