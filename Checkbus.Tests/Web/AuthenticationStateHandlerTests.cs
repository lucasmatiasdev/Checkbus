using System.Security.Claims;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Extensions;
using Checkbus.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="AuthenticationStateHandler"/> attaches the stored <c>"access_token"</c>
/// claim as an <c>Authorization: Bearer</c> header on outgoing requests. Per the Microsoft
/// "Application scope handler" pattern, the handler resolves <c>AuthenticationStateProvider</c>
/// from the <see cref="IServiceProvider"/> captured in <c>HttpRequestMessage.Options</c> under
/// <see cref="ApplicationScopeHandlerExtensions.ScopeKey"/> — not via constructor injection —
/// so these tests set that request option directly, exactly as <see cref="ApplicationScopeHandler"/>
/// would in the real pipeline.
/// </summary>
public class AuthenticationStateHandlerTests
{
    private static HttpRequestMessage CreateRequestWithScope(ClaimsPrincipal principal)
    {
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(
            new FakeAuthenticationStateProvider(principal));
        var scopeServiceProvider = services.BuildServiceProvider();

        var request = new HttpRequestMessage(HttpMethod.Get, "https://apiservice.local/api/auth/me");
        request.Options.Set(ApplicationScopeHandlerExtensions.ScopeKey, (IServiceProvider)scopeServiceProvider);
        return request;
    }

    [Fact]
    public async Task SendAsync_WhenAccessTokenClaimPresent_SetsAuthorizationBearerHeader()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("access_token", "test-jwt-token")], "TestAuthType"));
        var request = CreateRequestWithScope(principal);
        var innerHandler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var handler = new AuthenticationStateHandler { InnerHandler = innerHandler };
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.NotNull(innerHandler.CapturedRequest);
        Assert.Equal("Bearer", innerHandler.CapturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("test-jwt-token", innerHandler.CapturedRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SendAsync_WhenAccessTokenClaimAbsent_DoesNotSetAuthorizationHeader()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "jdoe")], "TestAuthType"));
        var request = CreateRequestWithScope(principal);
        var innerHandler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var handler = new AuthenticationStateHandler { InnerHandler = innerHandler };
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.NotNull(innerHandler.CapturedRequest);
        Assert.Null(innerHandler.CapturedRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_ForwardsRequestToInnerHandlerUnmodifiedOtherwise()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("access_token", "another-token")], "TestAuthType"));
        var request = CreateRequestWithScope(principal);
        request.Method = HttpMethod.Post;
        request.Headers.Add("X-Custom-Header", "custom-value");
        var innerHandler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Accepted));
        var handler = new AuthenticationStateHandler { InnerHandler = innerHandler };
        using var invoker = new HttpMessageInvoker(handler);

        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Same(request, innerHandler.CapturedRequest);
        Assert.Equal(HttpMethod.Post, innerHandler.CapturedRequest!.Method);
        Assert.Equal("custom-value", innerHandler.CapturedRequest.Headers.GetValues("X-Custom-Header").Single());
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}
