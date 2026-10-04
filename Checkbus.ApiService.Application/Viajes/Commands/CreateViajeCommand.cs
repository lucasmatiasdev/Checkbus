using Checkbus.ApiService.Application.Viajes.Queries;
using MediatR;

namespace Checkbus.ApiService.Application.Viajes.Commands
{
    public class CreateViajeCommand : IRequest<ViajeDto>
    {
        public required Guid VehicleId { get; set; }
        public required Guid ChoferId { get; set; }
        public required Guid EventoId { get; set; }
        public required DateTime FechaSalida { get; set; }
        public required DateTime FechaLlegada { get; set; }
        public required decimal Precio { get; set; }

        // Ordered: first = origin, last = destination, everything else = intermediate. Orden/Tipo
        // are deliberately absent from CreateViajeStopCommand below — both are computed
        // server-side from this list's position, never accepted from the client.
        public required IReadOnlyList<CreateViajeStopCommand> Stops { get; set; }

        // Deliberately ABSENT: OrganizationId (from the validated token, like CreateVehicleCommand)
        // and TiempoEstimado/DistanciaKm (always computed server-side via IDirectionsService,
        // never trusted from the client).
    }

    // Raw picked-location fields for one ordered stop, crossing the wire from Checkbus.Web's
    // PickedLocation (see R3) — the API-side equivalent of CreateEventoCommand's flattened
    // Ubicacion* fields. The handler resolves/dedupes the real Ubicacion row from these.
    public class CreateViajeStopCommand
    {
        public required string Nombre { get; set; }
        public required string Direccion { get; set; }
        public required string PlaceId { get; set; }
        public required double Latitud { get; set; }
        public required double Longitud { get; set; }
    }
}
