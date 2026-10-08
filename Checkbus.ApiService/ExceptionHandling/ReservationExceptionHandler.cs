using Checkbus.ApiService.Domain.Exceptions.Reservations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.ExceptionHandling
{
    // Maps the public reservation-create surface's domain exceptions (odd/tasks/reservas-publicas.md
    // R4). Separate from TripExceptionHandler: SeatsNoLongerAvailableException is a Reservation-
    // domain concept, not a Trip one (the Trip itself is fine; a specific Seat just got taken by
    // someone else), same reasoning as UserConflictExceptionHandler being its own handler rather
    // than folded into AuthExceptionHandler.
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
