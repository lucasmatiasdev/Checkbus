using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // One command used by all boarding validation entry points (odd/tasks/reservas-publicas.md
    // Fase 3 / A4, extended by odd/tasks/validacion-embarque-qr.md): a code-entry field that
    // parses a typed/pasted Reservation.Id, a row clicked from the search results, or a camera
    // QR scan decoding that same id — only Method differs (Codigo vs Busqueda vs Qr).
    // Deliberately carries no TripId: the handler resolves the owning Trip from
    // ReservationId itself and verifies ownership from there, same as
    // GetPublicReservationsConfirmationQueryHandler resolving Trip from Reservation rather than
    // the caller supplying it directly.
    public class ValidateBoardingCommand : IRequest
    {
        public required Guid ReservationId { get; set; }
        public required ValidationMethod Method { get; set; }
    }
}
