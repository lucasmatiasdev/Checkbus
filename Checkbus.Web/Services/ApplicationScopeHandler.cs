using Checkbus.Web.Extensions;

namespace Checkbus.Web.Services;

/// <summary>
/// Captures the calling (circuit-scoped) <see cref="IServiceProvider"/> on each outgoing
/// request, so downstream <see cref="DelegatingHandler"/>s created by
/// <see cref="IHttpMessageHandlerFactory"/> in their own separate DI scope — such as
/// <see cref="AuthenticationStateHandler"/> — can resolve circuit-scoped services like
/// <c>AuthenticationStateProvider</c> from the request instead of their own constructor.
/// Microsoft-documented "Application scope handler" pattern.
/// </summary>
public class ApplicationScopeHandler(IServiceProvider serviceProvider) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Options.Set(ApplicationScopeHandlerExtensions.ScopeKey, serviceProvider);
        return base.SendAsync(request, cancellationToken);
    }
}
