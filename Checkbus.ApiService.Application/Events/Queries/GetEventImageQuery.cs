using MediatR;

namespace Checkbus.ApiService.Application.Events.Queries
{
    public class GetEventImageQuery : IRequest<EventImageFileDto>
    {
        public required Guid EventId { get; set; }
    }
}
