using Checkbus.ApiService.Application.DriverRequirements.Commands;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.DriverRequirements;

namespace Checkbus.Tests.DriverRequirements;

public class ValidateDriverRequirementCommandHandlerTests
{
    private sealed class FakeDriverRequirementRepository : IDriverRequirementRepository
    {
        public IReadOnlyList<DriverRequirement> Requirements { get; set; } = [];
        public List<DriverRequirement> UpdatedRequirements { get; } = [];

        public Task<IReadOnlyList<DriverRequirement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(Requirements);

        public Task AddRangeAsync(IEnumerable<DriverRequirement> requirements, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateDriverRequirementCommandHandler.");

        public Task UpdateAsync(DriverRequirement requirement, CancellationToken cancellationToken)
        {
            UpdatedRequirements.Add(requirement);
            return Task.CompletedTask;
        }

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateDriverRequirementCommandHandler.");
    }

    private sealed class FakeUserRepository(User? target) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateDriverRequirementCommandHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(target);

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateDriverRequirementCommandHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateDriverRequirementCommandHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateDriverRequirementCommandHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateDriverRequirementCommandHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateDriverRequirementCommandHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? userId, Guid? organizationId, string? role) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => organizationId;
        public string? Role => role;
        public string? Email => "caller@checkbus-demo.com";
    }

    private static User CreateUser(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jose",
        Surname = "Diaz",
        Email = "jose.diaz@checkbus-demo.com",
        PasswordHash = "hashed-password",
        DocumentNumber = "40123456",
        Role = Role.Chofer,
        OrganizationId = organizationId,
        IsActive = true
    };

    private static DriverRequirement CreateRequirement(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = DriverRequirementType.LicenciaConducir,
        Status = DriverRequirementStatus.Pendiente,
        DocumentPresent = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static ValidateDriverRequirementCommand CreateCommand(Guid targetUserId, bool approved) => new()
    {
        TargetUserId = targetUserId,
        Type = DriverRequirementType.LicenciaConducir,
        Approved = approved
    };

    [Theory]
    [InlineData(true, DriverRequirementStatus.Apto)]
    [InlineData(false, DriverRequirementStatus.NoApto)]
    public async Task Handle_Admin_SetsExpectedStatus(bool approved, DriverRequirementStatus expectedStatus)
    {
        var organizationId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var target = CreateUser(organizationId);
        var repository = new FakeDriverRequirementRepository { Requirements = [CreateRequirement(targetUserId)] };
        var handler = new ValidateDriverRequirementCommandHandler(
            repository,
            new FakeUserRepository(target),
            new FakeCurrentUserService(Guid.NewGuid(), organizationId, "Administrador"));

        await handler.Handle(CreateCommand(targetUserId, approved), TestContext.Current.CancellationToken);

        var updated = Assert.Single(repository.UpdatedRequirements);
        Assert.Equal(expectedStatus, updated.Status);
    }

    [Fact]
    public async Task Handle_AdminCrossOrganization_ThrowsDriverRequirementNotFoundException()
    {
        var target = CreateUser(Guid.NewGuid());
        var repository = new FakeDriverRequirementRepository();
        var handler = new ValidateDriverRequirementCommandHandler(
            repository,
            new FakeUserRepository(target),
            new FakeCurrentUserService(Guid.NewGuid(), Guid.NewGuid(), "Administrador"));

        await Assert.ThrowsAsync<DriverRequirementNotFoundException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid(), approved: true), TestContext.Current.CancellationToken));

        Assert.Empty(repository.UpdatedRequirements);
    }

    [Theory]
    [InlineData("Chofer")]
    [InlineData("Planificador")]
    [InlineData("Mecanico")]
    public async Task Handle_NonAdmin_IsDenied(string role)
    {
        var targetUserId = Guid.NewGuid();
        var repository = new FakeDriverRequirementRepository { Requirements = [CreateRequirement(targetUserId)] };
        // Not even the owning Chofer may self-approve: the caller's id matches
        // targetUserId here, proving the check is role-only, not a self-service fallback.
        var handler = new ValidateDriverRequirementCommandHandler(
            repository,
            new FakeUserRepository(target: null),
            new FakeCurrentUserService(targetUserId, Guid.NewGuid(), role));

        await Assert.ThrowsAsync<DriverRequirementAccessDeniedException>(
            () => handler.Handle(CreateCommand(targetUserId, approved: true), TestContext.Current.CancellationToken));

        Assert.Empty(repository.UpdatedRequirements);
    }
}
