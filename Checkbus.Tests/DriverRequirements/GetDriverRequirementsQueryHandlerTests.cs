using Checkbus.ApiService.Application.DriverRequirements.Queries;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.DriverRequirements;

namespace Checkbus.Tests.DriverRequirements;

public class GetDriverRequirementsQueryHandlerTests
{
    private sealed class FakeDriverRequirementRepository : IDriverRequirementRepository
    {
        public IReadOnlyList<DriverRequirement> Requirements { get; set; } = [];

        public Task<IReadOnlyList<DriverRequirement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(Requirements);

        public Task AddRangeAsync(IEnumerable<DriverRequirement> requirements, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverRequirementsQueryHandler.");

        public Task UpdateAsync(DriverRequirement requirement, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverRequirementsQueryHandler.");

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverRequirementsQueryHandler.");
    }

    private sealed class FakeUserRepository(User? target) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverRequirementsQueryHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(target);

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverRequirementsQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverRequirementsQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverRequirementsQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverRequirementsQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetDriverRequirementsQueryHandler.");
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
        Status = DriverRequirementStatus.Apto,
        IssueDate = new DateOnly(2026, 1, 1),
        ExpirationDate = new DateOnly(2027, 1, 1),
        DocumentPresent = true,
        FileKey = "driver-requirements/some/key.pdf",
        FileContentType = "application/pdf",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_SelfAccess_IsAllowed()
    {
        var targetUserId = Guid.NewGuid();
        var repository = new FakeDriverRequirementRepository { Requirements = [CreateRequirement(targetUserId)] };
        var handler = new GetDriverRequirementsQueryHandler(
            repository,
            new FakeUserRepository(target: null),
            new FakeCurrentUserService(targetUserId, Guid.NewGuid(), "Chofer"));

        var result = await handler.Handle(
            new GetDriverRequirementsQuery { TargetUserId = targetUserId }, TestContext.Current.CancellationToken);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_AdminSameOrganization_IsAllowed()
    {
        var organizationId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var target = CreateUser(organizationId);
        var repository = new FakeDriverRequirementRepository { Requirements = [CreateRequirement(targetUserId)] };
        var handler = new GetDriverRequirementsQueryHandler(
            repository,
            new FakeUserRepository(target),
            new FakeCurrentUserService(Guid.NewGuid(), organizationId, "Administrador"));

        var result = await handler.Handle(
            new GetDriverRequirementsQuery { TargetUserId = targetUserId }, TestContext.Current.CancellationToken);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_AdminCrossOrganization_ThrowsDriverRequirementNotFoundException()
    {
        var target = CreateUser(Guid.NewGuid()); // different organization than the caller
        var repository = new FakeDriverRequirementRepository();
        var handler = new GetDriverRequirementsQueryHandler(
            repository,
            new FakeUserRepository(target),
            new FakeCurrentUserService(Guid.NewGuid(), Guid.NewGuid(), "Administrador"));

        await Assert.ThrowsAsync<DriverRequirementNotFoundException>(() => handler.Handle(
            new GetDriverRequirementsQuery { TargetUserId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_AdminTargetMissing_ThrowsDriverRequirementNotFoundException()
    {
        var repository = new FakeDriverRequirementRepository();
        var handler = new GetDriverRequirementsQueryHandler(
            repository,
            new FakeUserRepository(target: null),
            new FakeCurrentUserService(Guid.NewGuid(), Guid.NewGuid(), "Administrador"));

        await Assert.ThrowsAsync<DriverRequirementNotFoundException>(() => handler.Handle(
            new GetDriverRequirementsQuery { TargetUserId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("Chofer")]
    [InlineData("Planificador")]
    [InlineData("Mecanico")]
    public async Task Handle_NonAdminNonSelf_ThrowsDriverRequirementAccessDeniedException(string role)
    {
        var repository = new FakeDriverRequirementRepository();
        var handler = new GetDriverRequirementsQueryHandler(
            repository,
            new FakeUserRepository(target: null),
            new FakeCurrentUserService(Guid.NewGuid(), Guid.NewGuid(), role));

        await Assert.ThrowsAsync<DriverRequirementAccessDeniedException>(() => handler.Handle(
            new GetDriverRequirementsQuery { TargetUserId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ValidRequest_DtoExcludesFileKeyAndFileContentType()
    {
        // Compile-time proof: DriverRequirementDto has no FileKey/FileContentType members
        // at all, so the file is never reachable except through the dedicated download
        // endpoint. This test asserts that absence by reflection, in addition to the
        // compiler already enforcing it.
        var dtoProperties = typeof(DriverRequirementDto).GetProperties().Select(p => p.Name).ToArray();

        Assert.DoesNotContain("FileKey", dtoProperties);
        Assert.DoesNotContain("FileContentType", dtoProperties);

        var targetUserId = Guid.NewGuid();
        var repository = new FakeDriverRequirementRepository { Requirements = [CreateRequirement(targetUserId)] };
        var handler = new GetDriverRequirementsQueryHandler(
            repository,
            new FakeUserRepository(target: null),
            new FakeCurrentUserService(targetUserId, Guid.NewGuid(), "Chofer"));

        var result = await handler.Handle(
            new GetDriverRequirementsQuery { TargetUserId = targetUserId }, TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(DriverRequirementType.LicenciaConducir, dto.Type);
        Assert.Equal(DriverRequirementStatus.Apto, dto.Status);
        Assert.True(dto.DocumentPresent);
    }
}
