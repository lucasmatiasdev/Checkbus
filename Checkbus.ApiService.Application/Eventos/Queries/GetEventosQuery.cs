using MediatR;

namespace Checkbus.ApiService.Application.Eventos.Queries
{
    // Unscoped — Evento is a shared catalog (no OrganizationId), so there is nothing to
    // tenant-filter by. No parameters: every Planificador/Administrador sees every Evento.
    public class GetEventosQuery : IRequest<IReadOnlyList<EventoDto>>
    {
    }
}
