using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Tenancy;

namespace Checkbus.Tests.Infrastructure;

/// <summary>
/// Shared <see cref="User"/> fixture builder for auth tests that only need a
/// user with a given role to mint a JWT — the Organization details are fixed
/// dummies, not under test.
/// </summary>
public static class TestUserFactory
{
    public static User CreateUser(string role) => new()
    {
        Id = Guid.NewGuid(),
        Username = "jdoe",
        Email = "jdoe@example.com",
        PasswordHash = "hashed-password",
        DocumentNumber = "12345678",
        Role = role,
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
        IsActive = true
    };
}
