using Microsoft.AspNetCore.Antiforgery;

namespace Checkbus.UI.Endpoints;

/// <summary>
/// Minimal API endpoints are not covered by <c>app.UseAntiforgery()</c>
/// automatically (unlike Razor Pages/MVC/Blazor SSR forms) — each endpoint
/// that accepts a plain HTML form post has to opt in explicitly.
/// </summary>
internal static class AntiforgeryEndpointExtensions
{
    public static TBuilder ValidateAntiforgery<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try
            {
                await antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest("Antiforgery token validation failed.");
            }

            return await next(context);
        });
    }
}
