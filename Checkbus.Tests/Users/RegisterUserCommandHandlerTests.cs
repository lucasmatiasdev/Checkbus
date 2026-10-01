using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Users.Commands;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Authentication;

namespace Checkbus.Tests.Users;

public class RegisterUserCommandHandlerTests
{
    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> AddedUsers { get; } = [];
        public bool DocumentNumberExists { get; set; }
        public IReadOnlyList<string> TakenEmails { get; set; } = [];

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => Task.FromResult<User?>(null);

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            AddedUsers.Add(user);
            return Task.CompletedTask;
        }

        public Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(DocumentNumberExists);

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken)
            => Task.FromResult(TakenEmails);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by RegisterUserCommandHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by RegisterUserCommandHandler.");
    }

    private sealed class FakeOrganizationRepository(string? slug) : IOrganizationRepository
    {
        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(slug);
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Administrador";
        public string? Email => "admin@checkbus-demo.com";
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string? ReceivedPlaintext { get; private set; }

        public string Hash(User user, string password)
        {
            ReceivedPlaintext = password;
            return "hashed:" + password;
        }

        public bool Verify(string hash, string password) => hash == "hashed:" + password;
    }

    private static RegisterUserCommand CreateCommand() => new()
    {
        Name = "Jose",
        Surname = "Diaz",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "40123456",
        Role = Role.Chofer
    };

    [Fact]
    public async Task Handle_ValidRequest_ResolvesOrganizationFromClaimOnly()
    {
        // RegisterUserCommand has no OrganizationId field at all, so the only way
        // the handler can learn the tenant is from ICurrentUserService (anti-IDOR).
        var organizationId = Guid.NewGuid();
        var handler = new RegisterUserCommandHandler(
            new FakeUserRepository(),
            new FakeOrganizationRepository("checkbus-demo"),
            new FakeCurrentUserService(organizationId),
            new FakePasswordHasher());

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(organizationId, result.OrganizationId);
    }

    [Fact]
    public async Task Handle_ValidRequest_SetsMustChangePasswordTrue()
    {
        var handler = new RegisterUserCommandHandler(
            new FakeUserRepository(),
            new FakeOrganizationRepository("checkbus-demo"),
            new FakeCurrentUserService(Guid.NewGuid()),
            new FakePasswordHasher());

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.True(result.MustChangePassword);
    }

    [Fact]
    public async Task Handle_ValidRequest_SetsExplicitAuditTimestampsApproximatelyNow()
    {
        var userRepository = new FakeUserRepository();
        var handler = new RegisterUserCommandHandler(
            userRepository,
            new FakeOrganizationRepository("checkbus-demo"),
            new FakeCurrentUserService(Guid.NewGuid()),
            new FakePasswordHasher());

        var before = DateTime.UtcNow;
        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);
        var after = DateTime.UtcNow;

        var created = Assert.Single(userRepository.AddedUsers);
        Assert.NotEqual(default, created.CreatedAt);
        Assert.NotEqual(default, created.UpdatedAt);
        Assert.InRange(created.CreatedAt, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.InRange(created.UpdatedAt, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public async Task Handle_ValidRequest_HashesDocumentNumberAsInitialPassword()
    {
        var passwordHasher = new FakePasswordHasher();
        var handler = new RegisterUserCommandHandler(
            new FakeUserRepository(),
            new FakeOrganizationRepository("checkbus-demo"),
            new FakeCurrentUserService(Guid.NewGuid()),
            passwordHasher);

        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal("40123456", passwordHasher.ReceivedPlaintext);
    }

    [Fact]
    public async Task Handle_DuplicateDocumentNumberInOrganization_ThrowsDocumentNumberAlreadyRegisteredException()
    {
        var userRepository = new FakeUserRepository { DocumentNumberExists = true };
        var handler = new RegisterUserCommandHandler(
            userRepository,
            new FakeOrganizationRepository("checkbus-demo"),
            new FakeCurrentUserService(Guid.NewGuid()),
            new FakePasswordHasher());

        await Assert.ThrowsAsync<DocumentNumberAlreadyRegisteredException>(
            () => handler.Handle(CreateCommand(), TestContext.Current.CancellationToken));

        Assert.Empty(userRepository.AddedUsers);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new RegisterUserCommandHandler(
            new FakeUserRepository(),
            new FakeOrganizationRepository("checkbus-demo"),
            new FakeCurrentUserService(organizationId: null),
            new FakePasswordHasher());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_OrganizationRowMissing_ThrowsInvalidOperationException()
    {
        var handler = new RegisterUserCommandHandler(
            new FakeUserRepository(),
            new FakeOrganizationRepository(slug: null),
            new FakeCurrentUserService(Guid.NewGuid()),
            new FakePasswordHasher());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_GeneratesEmailFromNormalizedNameSurnameAndOrganizationSlug()
    {
        var userRepository = new FakeUserRepository();
        var handler = new RegisterUserCommandHandler(
            userRepository,
            new FakeOrganizationRepository("checkbus-demo"),
            new FakeCurrentUserService(Guid.NewGuid()),
            new FakePasswordHasher());

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal("jose.diaz@checkbus-demo.com", result.Email);
        Assert.Equal("jose.diaz@checkbus-demo.com", Assert.Single(userRepository.AddedUsers).Email);
    }

    [Fact]
    public async Task Handle_CollisionInTakenEmails_AppliesDisambiguationSuffix()
    {
        var userRepository = new FakeUserRepository
        {
            TakenEmails = ["jose.diaz@checkbus-demo.com"]
        };
        var handler = new RegisterUserCommandHandler(
            userRepository,
            new FakeOrganizationRepository("checkbus-demo"),
            new FakeCurrentUserService(Guid.NewGuid()),
            new FakePasswordHasher());

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal("jose.diaz1@checkbus-demo.com", result.Email);
    }

    [Fact]
    public async Task Handle_AdminCreatingAdmin_SucceedsScopedToCallersOrganization()
    {
        var organizationId = Guid.NewGuid();
        var handler = new RegisterUserCommandHandler(
            new FakeUserRepository(),
            new FakeOrganizationRepository("checkbus-demo"),
            new FakeCurrentUserService(organizationId),
            new FakePasswordHasher());
        var command = CreateCommand();
        command.Role = Role.Administrador;

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal("Administrador", result.Role);
        Assert.Equal(organizationId, result.OrganizationId);
    }
}
