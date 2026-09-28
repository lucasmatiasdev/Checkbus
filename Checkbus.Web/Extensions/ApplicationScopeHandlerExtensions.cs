using Checkbus.Web.Services;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Checkbus.Web.Extensions;

/// <summary>
/// Registers the Microsoft-documented "Application scope handler" pattern onto a named
/// <see cref="HttpClient"/> (see
/// https://learn.microsoft.com/aspnet/core/blazor/security/additional-scenarios#access-authenticationstateprovider-in-outgoing-request-middleware).
/// <see cref="IHttpClientFactory"/> creates <see cref="DelegatingHandler"/> instances in its own
/// internal DI scope, separate from the Blazor circuit's scope, so a handler that needs
/// circuit-scoped services (such as <c>AuthenticationStateProvider</c>) cannot resolve them via
/// constructor injection. Consumers must instead resolve the client as the keyed scoped service
/// registered here — via <see cref="IServiceProvider.GetRequiredKeyedService{T}"/> or
/// <c>[FromKeyedServices]</c> — so <see cref="ApplicationScopeHandler"/> can capture the correct,
/// circuit-scoped <see cref="IServiceProvider"/> on each outgoing request.
/// </summary>
public static class ApplicationScopeHandlerExtensions
{
    public static readonly HttpRequestOptionsKey<IServiceProvider> ScopeKey = new("ApplicationScope");

    public static IHttpClientBuilder AddApplicationScopeHandler(this IHttpClientBuilder builder)
    {
        var name = builder.Name;

        builder.Services.AddTransient<ApplicationScopeHandler>();

        builder.Services.AddKeyedScoped<HttpClient>(name, (serviceProvider, _) =>
        {
            var handler = serviceProvider.GetRequiredService<ApplicationScopeHandler>();
            handler.InnerHandler = serviceProvider
                .GetRequiredService<IHttpMessageHandlerFactory>()
                .CreateHandler(name);

            var client = new HttpClient(handler, disposeHandler: false);

            var options = serviceProvider
                .GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>()
                .Get(name);

            foreach (var action in options.HttpClientActions)
            {
                action(client);
            }

            return client;
        });

        return builder;
    }
}
