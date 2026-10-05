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

        // Raw picked-location fields crossing the wire from Checkbus.Web's PickedLocation (see
        // R3) — the API-side equivalent of its Nombre/Direccion/PlaceId/Latitud/Longitud. The
        // handler resolves/dedupes the real Location row from these before creating the Event.
        public required string LocationName { get; set; }
        public required string Address { get; set; }
        public required string PlaceId { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
    }
}
