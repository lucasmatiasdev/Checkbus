using Checkbus.ApiService.Domain.Exceptions.Boarding;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.ExceptionHandling
{

    public sealed class BoardingExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            switch (exception)
            {
                case BoardingRecordNotFoundException notFoundException:
                    await WriteNotFoundAsync(httpContext, notFoundException.Message, cancellationToken);
                    return true;

                case BoardingAlreadyValidatedException alreadyValidatedException:
                    await WriteConflictAsync(httpContext, alreadyValidatedException.Message, cancellationToken);
                    return true;

                case ReservationNotEligibleForBoardingException notEligibleException:
                    await WriteConflictAsync(httpContext, notEligibleException.Message, cancellationToken);
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
