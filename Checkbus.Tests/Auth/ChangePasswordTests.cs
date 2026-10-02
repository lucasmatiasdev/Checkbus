using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.Auth;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void NewPassword_Empty_IsRejected()
    {
        var command = new ChangePasswordCommand { NewPassword = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void NewPassword_TooShort_IsRejected()
    {
        var command = new ChangePasswordCommand { NewPassword = "abcd" }; // 4 chars

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void NewPassword_EightOrMoreChars_IsAccepted()
    {
        var command = new ChangePasswordCommand { NewPassword = "abcdefgh" }; // 8 chars

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void NewPassword_TooLong_IsRejected()
    {
        var command = new ChangePasswordCommand { NewPassword = new string('a', 129) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }
}

public class ChangePasswordCommandHandlerTests
{
    private sealed class FakeUserRepository(Dictionary<Guid, User> usersById) : IUserRepository
    {
        public List<User> UpdatedUsers { get; } = [];

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ChangePasswordCommandHandler.");

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ChangePasswordCommandHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ChangePasswordCommandHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ChangePasswordCommandHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(usersById.TryGetValue(id, out var user) ? user : null);

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
        {
            UpdatedUsers.Add(user);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ChangePasswordCommandHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => Guid.NewGuid();
        public string? Role => "Chofer";
        public string? Email => "jdoe@example.com";
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

    private static User CreateUser(Guid id, bool mustChangePassword = true) => new()
    {
        Id = id,
        Name = "Jane",
        Surname = "Doe",
        Email = "jdoe@example.com",
        PasswordHash = "old-hash",
        DocumentNumber = "12345678",
        Role = Role.Chofer,
        OrganizationId = Guid.NewGuid(),
        IsActive = true,
        MustChangePassword = mustChangePassword,
        CreatedAt = DateTime.UtcNow.AddDays(-1),
        UpdatedAt = DateTime.UtcNow.AddDays(-1)
    };

    [Fact]
    public async Task Handle_ValidRequest_UpdatesPasswordHashClearsFlagAndBumpsUpdatedAt()
    {
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, mustChangePassword: true);
        var beforeUpdatedAt = user.UpdatedAt;
        var repository = new FakeUserRepository(new Dictionary<Guid, User> { [userId] = user });
        var hasher = new FakePasswordHasher();
        var handler = new ChangePasswordCommandHandler(repository, new FakeCurrentUserService(userId), hasher);
        var command = new ChangePasswordCommand { NewPassword = "new-password-123" };

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var updated = Assert.Single(repository.UpdatedUsers);
        Assert.Same(user, updated);
        Assert.Equal("new-password-123", hasher.ReceivedPlaintext);
        Assert.Equal("hashed:new-password-123", updated.PasswordHash);
        Assert.False(updated.MustChangePassword);
        Assert.True(updated.UpdatedAt > beforeUpdatedAt);
    }

    [Fact]
    public async Task Handle_NoMatchingUser_ThrowsUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeUserRepository([]);
        var handler = new ChangePasswordCommandHandler(repository, new FakeCurrentUserService(userId), new FakePasswordHasher());
        var command = new ChangePasswordCommand { NewPassword = "new-password-123" };

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_MissingUserIdClaim_ThrowsUserNotFoundException()
    {
        var repository = new FakeUserRepository([]);
        var handler = new ChangePasswordCommandHandler(repository, new FakeCurrentUserService(userId: null), new FakePasswordHasher());
        var command = new ChangePasswordCommand { NewPassword = "new-password-123" };

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_MultipleUsersStored_OnlyCallerUserIdIsUpdated()
    {
        // Anti-IDOR: ChangePasswordCommand has no user-identifying field at all, so the
        // only way to prove the right (and only the right) row is touched is via
        // ICurrentUserService.UserId — this test makes that explicit with a second,
        // untouched user present in the repository.
        var callerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var caller = CreateUser(callerId);
        var other = CreateUser(otherId);
        var repository = new FakeUserRepository(new Dictionary<Guid, User> { [callerId] = caller, [otherId] = other });
        var handler = new ChangePasswordCommandHandler(repository, new FakeCurrentUserService(callerId), new FakePasswordHasher());
        var command = new ChangePasswordCommand { NewPassword = "new-password-123" };

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var updated = Assert.Single(repository.UpdatedUsers);
        Assert.Equal(callerId, updated.Id);
        Assert.NotEqual(otherId, updated.Id);
    }
}

public class ChangePasswordHttpTests : IClassFixture<CheckbusApiFactory>
{
    private const string ChangePasswordPath = "/api/auth/change-password";

    private readonly CheckbusApiFactory _factory;

    public ChangePasswordHttpTests(CheckbusApiFactory factory)
    {
        _factory = factory;
    }

    private static string CreateToken(Role role) =>
        new JwtGenerator(CheckbusApiFactory.CreateTestJwtOptions()).GenerateToken(TestUserFactory.CreateUser(role));

    private HttpClient CreateAuthorizedClient(Role role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role));
        return client;
    }

    [Fact]
    public async Task ChangePassword_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            ChangePasswordPath, new { NewPassword = "a-valid-new-password" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WeakPassword_Returns400()
    {
        // FluentValidation runs inside the MediatR pipeline before any repository
        // access, so this short-circuits before CheckbusApiFactory's unreachable
        // dummy Postgres connection ever comes into play (same D-13 constraint
        // UsersControllerAuthorizationTests documents for POST api/users).
        var client = CreateAuthorizedClient(Role.Chofer);

        var response = await client.PostAsJsonAsync(
            ChangePasswordPath, new { NewPassword = "short" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_NonAdminRole_IsNotRejectedByAuthorizationFilter()
    {
        // The endpoint is [Authorize] only, not role-gated. CheckbusApiFactory cannot
        // reach a database (D-13), so a full 204 happy path is not provable here (it is
        // Phase 5's Aspire E2E scenario); what IS provable in this harness is that a
        // non-admin role is never rejected by the ASP.NET Core role-authorization
        // filter — i.e. the response is never 403, proving the endpoint carries no
        // [Authorize(Roles = ...)] restriction.
        var client = CreateAuthorizedClient(Role.Chofer);

        var response = await client.PostAsJsonAsync(
            ChangePasswordPath, new { NewPassword = "a-valid-new-password" }, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
