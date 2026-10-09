using MediatR;

namespace Checkbus.ApiService.Application.Events.Queries
{
    // Tenant comes exclusively from the authenticated caller's claim inside the handler — no
    // OrganizationId field here, same reasoning as GetVehiclesQuery/GetUsersQuery.
    public class GetEventsQuery : IRequest<IReadOnlyList<EventDto>>
    {
    }
}
