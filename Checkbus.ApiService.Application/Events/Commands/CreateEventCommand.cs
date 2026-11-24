using Checkbus.ApiService.Application.Events.Queries;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Events.Commands
{
    public class CreateEventCommand : IRequest<EventDto>
    {
        public required string Name { get; set; }
        public required EventType Type { get; set; }
        public required DateTime Date { get; set; }

        public required string LocationName { get; set; }
        public required string Address { get; set; }
        public required string PlaceId { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
    }
}
