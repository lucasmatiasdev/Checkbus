using Checkbus.ApiService.Domain.Exceptions.Reservations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.ExceptionHandling
{

    public sealed class ReservationExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            switch (exception)
            {
                case SeatsNoLongerAvailableException seatsNoLongerAvailableException:
                    await WriteConflictAsync(httpContext, seatsNoLongerAvailableException.Message, cancellationToken);
                    return true;

                case ReservationNotFoundException reservationNotFoundException:
                    await WriteNotFoundAsync(httpContext, reservationNotFoundException.Message, cancellationToken);
                    return true;

                case ReservationNotCancellableException reservationNotCancellableException:
                    await WriteConflictAsync(httpContext, reservationNotCancellableException.Message, cancellationToken);
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
