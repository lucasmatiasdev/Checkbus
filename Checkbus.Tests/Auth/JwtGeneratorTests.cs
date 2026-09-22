using System.Security.Claims;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Authentication.Authorization;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Checkbus.Tests.Auth;

public class JwtGeneratorTests
{
    private static readonly JwtOptions Options = new()
    {
        SigningKey = "super-secret-signing-key-for-tests-0123456789",
        Issuer = "checkbus-tests",
        Audience = "checkbus-tests-audience",
        ExpirationMinutes = 60
    };

    private static User CreateUser(string role) => new()
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
        IsActive = true
    };

    [Fact]
    public void GenerateToken_AdminUser_ContainsSingleRoleClaimWithAdminValue()
    {
        var generator = new JwtGenerator(Options);
        var user = CreateUser("Admin");

        var token = generator.GenerateToken(user);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Single(roleClaims);
        Assert.Equal("Admin", roleClaims[0].Value);
    }

    [Fact]
    public void GenerateToken_DriverUser_ContainsSingleRoleClaimWithDriverValue()
    {
        var generator = new JwtGenerator(Options);
        var user = CreateUser("Driver");

        var token = generator.GenerateToken(user);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Single(roleClaims);
        Assert.Equal("Driver", roleClaims[0].Value);
    }

    [Fact]
    public void GenerateToken_IncludesIdentityAndOrganizationClaims()
    {
        var generator = new JwtGenerator(Options);
        var user = CreateUser("Admin");

        var token = generator.GenerateToken(user);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Username, jwt.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(user.OrganizationId.ToString(), jwt.Claims.Single(c => c.Type == "OrganizationId").Value);
    }

    [Fact]
    public void GenerateToken_HonoursIssuerAudienceAndExpiry()
    {
        var generator = new JwtGenerator(Options);
        var user = CreateUser("Admin");
        var beforeGeneration = DateTime.UtcNow;

        var token = generator.GenerateToken(user);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        Assert.Equal(Options.Issuer, jwt.Issuer);
        Assert.Contains(Options.Audience, jwt.Audiences);
        var expectedExpiry = beforeGeneration.AddMinutes(Options.ExpirationMinutes);
        Assert.True(jwt.ValidTo > beforeGeneration);
        Assert.True(jwt.ValidTo <= expectedExpiry.AddSeconds(30));
    }
}
