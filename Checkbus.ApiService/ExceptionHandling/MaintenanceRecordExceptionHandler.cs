using Checkbus.ApiService.Domain.Exceptions.Maintenance;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.ExceptionHandling
{
    public sealed class MaintenanceRecordExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            switch (exception)
            {
                case MaintenanceRecordNotFoundException notFoundException:
                    await WriteNotFoundAsync(httpContext, notFoundException.Message, cancellationToken);
                    return true;

                default:
                    return false;
            }
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
