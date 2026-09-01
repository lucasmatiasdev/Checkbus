using System.Security.Claims;
using Checkbus.BEL.Auth;
using Checkbus.BLL.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.17 — <see cref="HttpContextTenantProvider"/> is the production
/// <c>ITenantProvider</c>: it must read <see cref="CheckbusClaimTypes.OrganizationId"/>
/// from the signed-in user's claims (issued at login, CU-01) and must never
/// run in system mode.
/// </summary>
public class HttpContextTenantProviderTests
{
    [Fact]
    public void CurrentOrganizationId_Reads_The_OrganizationId_Claim_From_The_Signed_In_User()
    {
        var claims = new[] { new Claim(CheckbusClaimTypes.OrganizationId, "42") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var sut = new HttpContextTenantProvider(accessor);

        Assert.Equal(42, sut.CurrentOrganizationId);
        Assert.False(sut.IsSystemMode);
    }

    [Fact]
    public void CurrentOrganizationId_Is_Null_When_There_Is_No_Signed_In_User()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var sut = new HttpContextTenantProvider(accessor);

        Assert.Null(sut.CurrentOrganizationId);
    }

    [Fact]
    public void CurrentOrganizationId_Is_Null_When_There_Is_No_Http_Context_At_All()
    {
        var accessor = new HttpContextAccessor();

        var sut = new HttpContextTenantProvider(accessor);

        Assert.Null(sut.CurrentOrganizationId);
    }
}
