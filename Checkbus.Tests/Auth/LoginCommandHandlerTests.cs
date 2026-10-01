using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Auth;

public class LoginCommandHandlerTests
{
    private static User CreateUser(bool isActive = true, Role role = Role.Administrador, bool mustChangePassword = false) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jane",
        Surname = "Doe",
        Email = "jdoe@example.com",
        PasswordHash = "hashed-password",
        DocumentNumber = "12345678",
        Role = role,
        OrganizationId = Guid.NewGuid(),
        IsActive = isActive,
        MustChangePassword = mustChangePassword
    };

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => Task.FromResult(user);

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by LoginCommandHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by LoginCommandHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by LoginCommandHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by LoginCommandHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by LoginCommandHandler.");
    }

    private sealed class FakePasswordHasher(bool verifyResult) : IPasswordHasher
    {
        public string Hash(User user, string password) => "hashed-password";
        public bool Verify(string hash, string password) => verifyResult;
    }

    private sealed class FakeJwtGenerator : IJwtGenerator
    {
        public User? ReceivedUser { get; private set; }
        public int CallCount { get; private set; }

        public string GenerateToken(User user)
        {
            ReceivedUser = user;
            CallCount++;
            return "fake-jwt-token";
        }
    }

    private sealed record LogEntry(LogLevel Level, IReadOnlyDictionary<string, object?> Fields);

    private sealed class FakeLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add(new LogEntry(logLevel,
                state is IReadOnlyList<KeyValuePair<string, object?>> v
                    ? v.ToDictionary(kv => kv.Key, kv => kv.Value)
                    : new Dictionary<string, object?>()));
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsUserNotFoundException()
    {
        var handler = new LoginCommandHandler(
            new FakeUserRepository(user: null),
            new FakePasswordHasher(verifyResult: true),
            new FakeJwtGenerator(),
            NullLogger<LoginCommandHandler>.Instance);
        var command = new LoginCommand { Email = "missing@example.com", Password = "password123" };

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var handler = new LoginCommandHandler(
            new FakeUserRepository(CreateUser()),
            new FakePasswordHasher(verifyResult: false),
            new FakeJwtGenerator(),
            NullLogger<LoginCommandHandler>.Instance);
        var command = new LoginCommand { Email = "jdoe@example.com", Password = "wrong-password" };

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUserInactiveException()
    {
        var handler = new LoginCommandHandler(
            new FakeUserRepository(CreateUser(isActive: false)),
            new FakePasswordHasher(verifyResult: true),
            new FakeJwtGenerator(),
            NullLogger<LoginCommandHandler>.Instance);
        var command = new LoginCommand { Email = "jdoe@example.com", Password = "password123" };

        await Assert.ThrowsAsync<UserInactiveException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ValidCredentials_GeneratesTokenAndReturnsResult()
    {
        var user = CreateUser(role: Role.Chofer);
        var jwtGenerator = new FakeJwtGenerator();
        var handler = new LoginCommandHandler(
            new FakeUserRepository(user),
            new FakePasswordHasher(verifyResult: true),
            jwtGenerator,
            NullLogger<LoginCommandHandler>.Instance);
        var command = new LoginCommand { Email = "jdoe@example.com", Password = "password123" };

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(1, jwtGenerator.CallCount);
        Assert.Same(user, jwtGenerator.ReceivedUser);
        Assert.Equal("fake-jwt-token", result.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.OrganizationId, result.OrganizationId);
        Assert.Equal("Chofer", result.Role);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task Handle_ValidCredentials_LogsLoginSucceededWithIdentityFields()
    {
        var user = CreateUser(role: Role.Chofer);
        var jwtGenerator = new FakeJwtGenerator();
        var logger = new FakeLogger<LoginCommandHandler>();
        var handler = new LoginCommandHandler(
            new FakeUserRepository(user),
            new FakePasswordHasher(verifyResult: true),
            jwtGenerator,
            logger);
        var command = new LoginCommand { Email = "jdoe@example.com", Password = "password123" };

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(user.Id, entry.Fields["UserId"]);
        Assert.Equal(user.OrganizationId, entry.Fields["OrganizationId"]);
        Assert.Equal("Chofer", entry.Fields["Role"]);
    }

    [Fact]
    public async Task Handle_UnknownEmail_LogsLoginFailedWithoutUserId()
    {
        var logger = new FakeLogger<LoginCommandHandler>();
        var handler = new LoginCommandHandler(
            new FakeUserRepository(user: null),
            new FakePasswordHasher(verifyResult: true),
            new FakeJwtGenerator(),
            logger);
        var command = new LoginCommand { Email = "missing@example.com", Password = "password123" };

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("UserNotFound", entry.Fields["FailureReason"]);
        Assert.False(entry.Fields.ContainsKey("UserId"));
    }

    [Fact]
    public async Task Handle_InactiveUser_LogsLoginFailedWithUserId()
    {
        var user = CreateUser(isActive: false);
        var logger = new FakeLogger<LoginCommandHandler>();
        var handler = new LoginCommandHandler(
            new FakeUserRepository(user),
            new FakePasswordHasher(verifyResult: true),
            new FakeJwtGenerator(),
            logger);
        var command = new LoginCommand { Email = "jdoe@example.com", Password = "password123" };

        await Assert.ThrowsAsync<UserInactiveException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("UserInactive", entry.Fields["FailureReason"]);
        Assert.Equal(user.Id, entry.Fields["UserId"]);
        Assert.Equal(user.OrganizationId, entry.Fields["OrganizationId"]);
    }

    [Fact]
    public async Task Handle_WrongPassword_LogsLoginFailedWithUserId()
    {
        var user = CreateUser();
        var logger = new FakeLogger<LoginCommandHandler>();
        var handler = new LoginCommandHandler(
            new FakeUserRepository(user),
            new FakePasswordHasher(verifyResult: false),
            new FakeJwtGenerator(),
            logger);
        var command = new LoginCommand { Email = "jdoe@example.com", Password = "wrong-password" };

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("InvalidCredentials", entry.Fields["FailureReason"]);
        Assert.Equal(user.Id, entry.Fields["UserId"]);
        Assert.Equal(user.OrganizationId, entry.Fields["OrganizationId"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ValidCredentials_ResultCarriesMustChangePasswordFlag(bool mustChangePassword)
    {
        var user = CreateUser(role: Role.Chofer, mustChangePassword: mustChangePassword);
        var handler = new LoginCommandHandler(
            new FakeUserRepository(user),
            new FakePasswordHasher(verifyResult: true),
            new FakeJwtGenerator(),
            NullLogger<LoginCommandHandler>.Instance);
        var command = new LoginCommand { Email = "jdoe@example.com", Password = "password123" };

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(mustChangePassword, result.MustChangePassword);
    }

    [Fact]
    public async Task Handle_AnyOutcome_NeverLogsSecretsOrEmail()
    {
        var user = CreateUser();
        var jwtGenerator = new FakeJwtGenerator();
        var logger = new FakeLogger<LoginCommandHandler>();
        var handler = new LoginCommandHandler(
            new FakeUserRepository(user),
            new FakePasswordHasher(verifyResult: true),
            jwtGenerator,
            logger);
        var command = new LoginCommand { Email = "jdoe@example.com", Password = "password123" };

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var forbiddenValues = new[]
        {
            user.Email,
            user.PasswordHash,
            command.Password,
            "fake-jwt-token"
        };

        foreach (var entry in logger.Entries)
        {
            foreach (var value in entry.Fields.Values)
            {
                if (value is string stringValue)
                {
                    Assert.DoesNotContain(forbiddenValues, forbidden => stringValue.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }
}
