using Checkbus.ApiService.Domain.Exceptions.Rutas;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.ExceptionHandling
{
    public sealed class ViajeExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            switch (exception)
            {
                case ViajeNotHabilitadoException notHabilitadoException:
                    await WriteHabilitacionFailedAsync(httpContext, notHabilitadoException.Message, cancellationToken);
                    return true;

                case ChoferNotFoundException notFoundException:
                    await WriteNotFoundAsync(httpContext, notFoundException.Message, cancellationToken);
                    return true;

                case EventoNotFoundException eventoNotFoundException:
                    await WriteNotFoundAsync(httpContext, eventoNotFoundException.Message, cancellationToken);
                    return true;

                case ViajeNotFoundException viajeNotFoundException:
                    await WriteNotFoundAsync(httpContext, viajeNotFoundException.Message, cancellationToken);
                    return true;

                default:
                    return false;
            }
        }

        // Written as a ValidationProblemDetails (not a plain ProblemDetails) so
        // Checkbus.Web's ViajeActionOutcome.ValidationFailed case can parse it exactly like a
        // FluentValidation 400 and surface the habilitación message directly to the user
        // (odd/tasks/rutas-publicacion.md: "the message must name which rule failed").
        private static Task WriteHabilitacionFailedAsync(HttpContext httpContext, string detail, CancellationToken cancellationToken)
        {
            var problem = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["Habilitacion"] = [detail]
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = detail
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        }

        private static Task WriteNotFoundAsync(HttpContext httpContext, string detail, CancellationToken cancellationToken)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = detail
            };

            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        }
    }
}
