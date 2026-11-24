using Checkbus.ApiService.Domain.Exceptions.Trips;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.ExceptionHandling
{
    public sealed class TripExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            switch (exception)
            {
                case TripNotEligibleException notEligibleException:
                    await WriteEligibilityFailedAsync(httpContext, notEligibleException.Message, cancellationToken);
                    return true;

                case DriverNotFoundException notFoundException:
                    await WriteNotFoundAsync(httpContext, notFoundException.Message, cancellationToken);
                    return true;

                case EventNotFoundException eventNotFoundException:
                    await WriteNotFoundAsync(httpContext, eventNotFoundException.Message, cancellationToken);
                    return true;

                case TripNotFoundException tripNotFoundException:
                    await WriteNotFoundAsync(httpContext, tripNotFoundException.Message, cancellationToken);
                    return true;

                default:
                    return false;
            }
        }

        private static Task WriteEligibilityFailedAsync(HttpContext httpContext, string detail, CancellationToken cancellationToken)
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
