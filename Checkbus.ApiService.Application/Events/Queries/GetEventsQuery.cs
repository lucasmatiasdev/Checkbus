using MediatR;

namespace Checkbus.ApiService.Application.Events.Queries
{

    public class GetEventsQuery : IRequest<IReadOnlyList<EventDto>>
    {
    }
}
