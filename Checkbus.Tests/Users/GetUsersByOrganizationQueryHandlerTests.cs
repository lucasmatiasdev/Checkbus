using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Users.Queries;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Exceptions.Tenancy;

namespace Checkbus.Tests.Users;

public class GetUsersByOrganizationQueryHandlerTests
{
    private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
    {
        public Guid? ReceivedOrganizationId { get; private set; }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            ReceivedOrganizationId = organizationId;
            return Task.FromResult(users);
        }
    }

    private sealed class FakeOrganizationRepository(string? callerSlug) : IOrganizationRepository
    {
        public Task AddAsync(Organization organization, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(callerSlug);

        public Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task<bool> CuitExistsAsync(string cuit, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");

        public Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersByOrganizationQueryHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "CheckbusSoporte";
        public string? Email => "soporte@checkbus-interno.com";
    }

    private static User CreateUser(Guid organizationId, string email) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jose",
        Surname = "Diaz",
        Email = email,
        PasswordHash = "hashed-password",
        DocumentNumber = "40123456",
        Role = Role.Planificador,
        OrganizationId = organizationId,
        IsActive = true
    };

    [Fact]
    public async Task Handle_CallerFromSentinelOrganization_ReturnsUsersOfRequestedOrganization_NotCallers()
    {
        var callerOrganizationId = Guid.NewGuid();
        var targetOrganizationId = Guid.NewGuid();
        var user = CreateUser(targetOrganizationId, "jose.diaz@checkbus-demo.com");
        var userRepository = new FakeUserRepository([user]);
        var handler = new GetUsersByOrganizationQueryHandler(
            userRepository,
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug),
            new FakeCurrentUserService(callerOrganizationId));

        var result = await handler.Handle(
            new GetUsersByOrganizationQuery { OrganizationId = targetOrganizationId },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(targetOrganizationId, userRepository.ReceivedOrganizationId);
    }

    [Fact]
    public async Task Handle_CallerOrganizationIsNotSentinel_ThrowsOrganizationAccessDeniedException()
    {
        var handler = new GetUsersByOrganizationQueryHandler(
            new FakeUserRepository([]),
            new FakeOrganizationRepository("otra-organizacion"),
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<OrganizationAccessDeniedException>(
            () => handler.Handle(
                new GetUsersByOrganizationQuery { OrganizationId = Guid.NewGuid() },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetUsersByOrganizationQueryHandler(
            new FakeUserRepository([]),
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug),
            new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new GetUsersByOrganizationQuery { OrganizationId = Guid.NewGuid() },
                TestContext.Current.CancellationToken));
    }
}
