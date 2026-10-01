using Checkbus.ApiService.Domain.Exceptions.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.ExceptionHandling
{
    public sealed class UserConflictExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            switch (exception)
            {
                case DocumentNumberAlreadyRegisteredException:
                case EmailAlreadyRegisteredException:
                case EmailGenerationExhaustedException:
                    await WriteConflictAsync(httpContext, exception.Message, cancellationToken);
                    return true;

                default:
                    return false;
            }
        }

        private static Task WriteConflictAsync(HttpContext httpContext, string detail, CancellationToken cancellationToken)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = detail
            };

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        }
    }
}
