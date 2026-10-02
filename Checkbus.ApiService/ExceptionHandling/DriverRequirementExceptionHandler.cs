using Checkbus.ApiService.Domain.Exceptions.DriverRequirements;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.ExceptionHandling
{
    public sealed class DriverRequirementExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            switch (exception)
            {
                case DriverRequirementAccessDeniedException:
                    await WriteForbiddenAsync(httpContext, cancellationToken);
                    return true;

                case DriverRequirementNotFoundException notFoundException:
                    await WriteNotFoundAsync(httpContext, notFoundException.Message, cancellationToken);
                    return true;

                default:
                    return false;
            }
        }

        private static Task WriteForbiddenAsync(HttpContext httpContext, CancellationToken cancellationToken)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "You are not allowed to perform this action."
            };

            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
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
