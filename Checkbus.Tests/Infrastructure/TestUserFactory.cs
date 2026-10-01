using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;

namespace Checkbus.Tests.Infrastructure;

/// <summary>
/// Shared <see cref="User"/> fixture builder for auth tests that only need a
/// user with a given role to mint a JWT — the Organization details are not
/// under test, so <see cref="User.Organization"/> is left unset.
/// </summary>
public static class TestUserFactory
{
    public static User CreateUser(Role role) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jane",
        Surname = "Doe",
        Email = "jdoe@example.com",
        PasswordHash = "hashed-password",
        DocumentNumber = "12345678",
        Role = role,
        OrganizationId = Guid.NewGuid(),
        IsActive = true
    };
}
