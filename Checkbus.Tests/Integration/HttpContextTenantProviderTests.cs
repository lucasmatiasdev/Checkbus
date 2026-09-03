using System.Security.Claims;
using Checkbus.BEL.Auth;
using Checkbus.BLL.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S2.17 — <see cref="HttpContextTenantProvider"/> is the production
/// <c>ITenantProvider</c>: it must read <see cref="CheckbusClaimTypes.OrganizationId"/>
/// from the signed-in user's claims (issued at login, CU-01) and must never
/// run in system mode. S3.7 — when <c>HttpContext</c> is unavailable (deep
/// interactive Blazor Server render), it must fall back to
/// <see cref="CircuitTenantState"/>.
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

        var sut = new HttpContextTenantProvider(accessor, new CircuitTenantState());

        Assert.Equal(42, sut.CurrentOrganizationId);
        Assert.False(sut.IsSystemMode);
    }

    [Fact]
    public void CurrentOrganizationId_Is_Null_When_There_Is_No_Signed_In_User_And_No_Circuit_Fallback()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var sut = new HttpContextTenantProvider(accessor, new CircuitTenantState());

        Assert.Null(sut.CurrentOrganizationId);
    }

    [Fact]
    public void CurrentOrganizationId_Is_Null_When_There_Is_No_Http_Context_At_All_And_No_Circuit_Fallback()
    {
        var accessor = new HttpContextAccessor();

        var sut = new HttpContextTenantProvider(accessor, new CircuitTenantState());

        Assert.Null(sut.CurrentOrganizationId);
    }

    [Fact]
    public void CurrentOrganizationId_Falls_Back_To_CircuitTenantState_When_Http_Context_Is_Unavailable()
    {
        var accessor = new HttpContextAccessor();
        var circuitTenantState = new CircuitTenantState();
        circuitTenantState.Capture(7);

        var sut = new HttpContextTenantProvider(accessor, circuitTenantState);

        Assert.Equal(7, sut.CurrentOrganizationId);
    }

    [Fact]
    public void CurrentOrganizationId_Prefers_The_Http_Context_Claim_Over_The_Circuit_Fallback()
    {
        var claims = new[] { new Claim(CheckbusClaimTypes.OrganizationId, "42") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var circuitTenantState = new CircuitTenantState();
        circuitTenantState.Capture(99);

        var sut = new HttpContextTenantProvider(accessor, circuitTenantState);

        Assert.Equal(42, sut.CurrentOrganizationId);
    }
}
