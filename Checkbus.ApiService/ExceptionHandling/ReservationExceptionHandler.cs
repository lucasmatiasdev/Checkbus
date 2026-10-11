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

                // Added for Fase 4 (Cancelación, odd/tasks/reservas-publicas.md C3): the
                // submitted Reservation.Id doesn't resolve to anything.
                case ReservationNotFoundException reservationNotFoundException:
                    await WriteNotFoundAsync(httpContext, reservationNotFoundException.Message, cancellationToken);
                    return true;

                // Added for Fase 4: either the Reservation's BoardingRecord is already
                // Abordado or fewer than 24h remain before Trip.DepartureDate. The message is
                // the user-facing Spanish text shown verbatim near the reservation row.
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
