using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Users.Queries;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;

namespace Checkbus.Tests.Users;

public class GetUsersQueryHandlerTests
{
    private sealed class FakeUserRepository : IUserRepository
    {
        public IReadOnlyList<User> Users { get; set; } = [];
        public Guid? ReceivedOrganizationId { get; private set; }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersQueryHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersQueryHandler.");

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetUsersQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            ReceivedOrganizationId = organizationId;
            return Task.FromResult(Users);
        }
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Administrador";
        public string? Email => "admin@checkbus-demo.com";
    }

    private static User CreateUser(Guid organizationId, string email, Role role) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jose",
        Surname = "Diaz",
        Email = email,
        PasswordHash = "hashed-password",
        DocumentNumber = "40123456",
        Role = role,
        OrganizationId = organizationId,
        IsActive = true
    };

    [Fact]
    public async Task Handle_ValidRequest_ReturnsRepositoryUsersMappedToDto()
    {
        var organizationId = Guid.NewGuid();
        var user = CreateUser(organizationId, "jose.diaz@checkbus-demo.com", Role.Chofer);
        var repository = new FakeUserRepository { Users = [user] };
        var handler = new GetUsersQueryHandler(repository, new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetUsersQuery(), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Name, dto.Name);
        Assert.Equal(user.Surname, dto.Surname);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(user.Role, dto.Role);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryWithCallersOrganizationId()
    {
        var organizationId = Guid.NewGuid();
        var repository = new FakeUserRepository();
        var handler = new GetUsersQueryHandler(repository, new FakeCurrentUserService(organizationId));

        await handler.Handle(new GetUsersQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(organizationId, repository.ReceivedOrganizationId);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetUsersQueryHandler(new FakeUserRepository(), new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetUsersQuery(), TestContext.Current.CancellationToken));
    }
}
