using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // "Cerrar embarque" (odd/tasks/reservas-publicas.md Fase 3 / A4) — a one-way bulk action:
    // every BoardingRecord still Pendiente for this Trip is moved to Ausente. Only available to
    // the Chofer on their own Trip's boarding screen, never reversible in this phase.
    public class CloseBoardingCommand : IRequest
    {
        public required Guid TripId { get; set; }
    }
}
