using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Authentication.Authorization;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Exceptions.Authentication;

namespace Checkbus.Tests.Auth;

public class LoginCommandHandlerTests
{
    private static User CreateUser(bool isActive = true, string role = "Admin") => new()
    {
        Id = Guid.NewGuid(),
        Username = "jdoe",
        Email = "jdoe@example.com",
        PasswordHash = "hashed-password",
        DocumentNumber = "12345678",
        RoleId = Guid.NewGuid(),
        Role = new Role { Id = Guid.NewGuid(), Name = role },
        OrganizationId = Guid.NewGuid(),
        Organization = new Organization
        {
            Id = Guid.NewGuid(),
            CUIT = "20-12345678-9",
            Name = "Acme",
            Slug = "acme",
            LogoUrl = "https://example.com/logo.png",
            IsActive = true
        },
        IsActive = isActive
    };

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => Task.FromResult(user);
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

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsUserNotFoundException()
    {
        var handler = new LoginCommandHandler(
            new FakeUserRepository(user: null),
            new FakePasswordHasher(verifyResult: true),
            new FakeJwtGenerator());
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
            new FakeJwtGenerator());
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
            new FakeJwtGenerator());
        var command = new LoginCommand { Email = "jdoe@example.com", Password = "password123" };

        await Assert.ThrowsAsync<UserInactiveException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ValidCredentials_GeneratesTokenAndReturnsResult()
    {
        var user = CreateUser(role: "Driver");
        var jwtGenerator = new FakeJwtGenerator();
        var handler = new LoginCommandHandler(
            new FakeUserRepository(user),
            new FakePasswordHasher(verifyResult: true),
            jwtGenerator);
        var command = new LoginCommand { Email = "jdoe@example.com", Password = "password123" };

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(1, jwtGenerator.CallCount);
        Assert.Same(user, jwtGenerator.ReceivedUser);
        Assert.Equal("fake-jwt-token", result.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.OrganizationId, result.OrganizationId);
        Assert.Equal("Driver", result.Role);
        Assert.Equal(user.Username, result.Username);
    }
}
