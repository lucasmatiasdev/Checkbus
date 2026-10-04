using MediatR;

namespace Checkbus.ApiService.Application.Events.Queries
{
    // Unscoped — Event is a shared catalog (no OrganizationId), so there is nothing to
    // tenant-filter by. No parameters: every Planificador/Administrador sees every Event.
    public class GetEventsQuery : IRequest<IReadOnlyList<EventDto>>
    {
    }
}
