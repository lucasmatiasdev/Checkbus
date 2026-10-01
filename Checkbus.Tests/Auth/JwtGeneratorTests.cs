using System.Security.Claims;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
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

    private static User CreateUser(Role role) => new()
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

    [Fact]
    public void GenerateToken_AdministradorUser_ContainsSingleRoleClaimWithAdministradorValue()
    {
        var generator = new JwtGenerator(Options);
        var user = CreateUser(Role.Administrador);

        var token = generator.GenerateToken(user);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Single(roleClaims);
        Assert.Equal("Administrador", roleClaims[0].Value);
    }

    [Fact]
    public void GenerateToken_ChoferUser_ContainsSingleRoleClaimWithChoferValue()
    {
        var generator = new JwtGenerator(Options);
        var user = CreateUser(Role.Chofer);

        var token = generator.GenerateToken(user);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Single(roleClaims);
        Assert.Equal("Chofer", roleClaims[0].Value);
    }

    [Fact]
    public void GenerateToken_IncludesIdentityAndOrganizationClaims()
    {
        var generator = new JwtGenerator(Options);
        var user = CreateUser(Role.Administrador);

        var token = generator.GenerateToken(user);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(user.OrganizationId.ToString(), jwt.Claims.Single(c => c.Type == "OrganizationId").Value);
    }

    [Fact]
    public void GenerateToken_HonoursIssuerAudienceAndExpiry()
    {
        var generator = new JwtGenerator(Options);
        var user = CreateUser(Role.Administrador);
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
