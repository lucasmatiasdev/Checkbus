using System.Security.Claims;
using Checkbus.ApiService.Services;
using Microsoft.AspNetCore.Http;

namespace Checkbus.Tests.Auth;

public class CurrentUserServiceTests
{
    private static IHttpContextAccessor CreateAccessor(HttpContext? httpContext) =>
        new HttpContextAccessor { HttpContext = httpContext };

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(IEnumerable<Claim> claims) =>
        new(new ClaimsIdentity(claims, "TestAuthType"));

    [Fact]
    public void AuthenticatedPrincipal_ReadsAllFourClaims()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(CurrentUserService.OrganizationIdClaimType,organizationId.ToString()),
            new Claim(ClaimTypes.Role, "Administrador"),
            new Claim(ClaimTypes.Name, "jdoe")
        };
        var httpContext = new DefaultHttpContext { User = CreateAuthenticatedPrincipal(claims) };
        var service = new CurrentUserService(CreateAccessor(httpContext));

        Assert.True(service.IsAuthenticated);
        Assert.Equal(userId, service.UserId);
        Assert.Equal(organizationId, service.OrganizationId);
        Assert.Equal("Administrador", service.Role);
        Assert.Equal("jdoe", service.Username);
    }

    [Fact]
    public void UnauthenticatedPrincipal_ReturnsFalseAndAllNull()
    {
        // An identity built without an authenticationType is, by ClaimsIdentity's own
        // contract, unauthenticated — even though it carries claims. This proves the
        // service gates on IsAuthenticated, not merely on claim presence.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(CurrentUserService.OrganizationIdClaimType,Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Administrador"),
            new Claim(ClaimTypes.Name, "jdoe")
        };
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) };
        var service = new CurrentUserService(CreateAccessor(httpContext));

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
        Assert.Null(service.OrganizationId);
        Assert.Null(service.Role);
        Assert.Null(service.Username);
    }

    [Fact]
    public void MalformedGuidClaims_ReturnNullWithoutThrowing()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
            new Claim(CurrentUserService.OrganizationIdClaimType,"also-not-a-guid"),
            new Claim(ClaimTypes.Role, "Administrador"),
            new Claim(ClaimTypes.Name, "jdoe")
        };
        var httpContext = new DefaultHttpContext { User = CreateAuthenticatedPrincipal(claims) };
        var service = new CurrentUserService(CreateAccessor(httpContext));

        var exception = Record.Exception(() =>
        {
            _ = service.UserId;
            _ = service.OrganizationId;
        });

        Assert.Null(exception);
        Assert.Null(service.UserId);
        Assert.Null(service.OrganizationId);
        // Unaffected claims still resolve normally — malformed handling is isolated per property.
        Assert.Equal("Administrador", service.Role);
        Assert.Equal("jdoe", service.Username);
    }

    [Fact]
    public void NoHttpContext_ReturnsFalseAndAllNullWithoutThrowing()
    {
        var service = new CurrentUserService(CreateAccessor(null));

        var exception = Record.Exception(() =>
        {
            _ = service.IsAuthenticated;
            _ = service.UserId;
            _ = service.OrganizationId;
            _ = service.Role;
            _ = service.Username;
        });

        Assert.Null(exception);
        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
        Assert.Null(service.OrganizationId);
        Assert.Null(service.Role);
        Assert.Null(service.Username);
    }
}
