using Checkbus.ApiService.Application.Eventos.Queries;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Eventos.Commands
{
    public class CreateEventoCommand : IRequest<EventoDto>
    {
        public required string Nombre { get; set; }
        public required EventoTipo Tipo { get; set; }
        public required DateTime Fecha { get; set; }

        // Raw picked-location fields crossing the wire from Checkbus.Web's PickedLocation (see
        // R3) — the API-side equivalent of its Nombre/Direccion/PlaceId/Latitud/Longitud. The
        // handler resolves/dedupes the real Ubicacion row from these before creating the Evento.
        public required string UbicacionNombre { get; set; }
        public required string Direccion { get; set; }
        public required string PlaceId { get; set; }
        public required double Latitud { get; set; }
        public required double Longitud { get; set; }
    }
}
