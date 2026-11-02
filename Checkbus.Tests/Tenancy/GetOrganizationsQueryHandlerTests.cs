using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Tenancy.Queries;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Exceptions.Tenancy;

namespace Checkbus.Tests.Tenancy;

public class GetOrganizationsQueryHandlerTests
{
    private sealed class FakeOrganizationRepository(string? callerSlug, IReadOnlyList<Organization> organizations) : IOrganizationRepository
    {
        public Task AddAsync(Organization organization, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetOrganizationsQueryHandler.");

        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(callerSlug);

        public Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetOrganizationsQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetOrganizationsQueryHandler.");

        public Task<bool> CuitExistsAsync(string cuit, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetOrganizationsQueryHandler.");

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetOrganizationsQueryHandler.");

        public Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(organizations);
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "CheckbusSoporte";
        public string? Email => "soporte@checkbus-interno.com";
    }

    private static Organization CreateOrganization(string name) => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = name,
        Slug = name.ToLowerInvariant(),
        LogoUrl = "",
        IsActive = true
    };

    [Fact]
    public async Task Handle_CallerFromSentinelOrganization_ReturnsAllOrganizations()
    {
        var sentinelOrganizationId = Guid.NewGuid();
        var organizationA = CreateOrganization("Organizacion A");
        var organizationB = CreateOrganization("Organizacion B");
        var handler = new GetOrganizationsQueryHandler(
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug, [organizationA, organizationB]),
            new FakeCurrentUserService(sentinelOrganizationId));

        var result = await handler.Handle(new GetOrganizationsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, dto => dto.Id == organizationA.Id && dto.Name == "Organizacion A");
        Assert.Contains(result, dto => dto.Id == organizationB.Id && dto.Name == "Organizacion B");
    }

    [Fact]
    public async Task Handle_CallerOrganizationIsNotSentinel_ThrowsOrganizationAccessDeniedException()
    {
        var callerOrganizationId = Guid.NewGuid();
        var handler = new GetOrganizationsQueryHandler(
            new FakeOrganizationRepository("otra-organizacion", []),
            new FakeCurrentUserService(callerOrganizationId));

        await Assert.ThrowsAsync<OrganizationAccessDeniedException>(
            () => handler.Handle(new GetOrganizationsQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetOrganizationsQueryHandler(
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug, []),
            new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetOrganizationsQuery(), TestContext.Current.CancellationToken));
    }
}
