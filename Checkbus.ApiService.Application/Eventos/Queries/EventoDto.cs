using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Eventos.Queries
{
    public class EventoDto
    {
        public required Guid Id { get; set; }
        public required string Nombre { get; set; }
        public EventoTipo Tipo { get; set; }
        public DateTime Fecha { get; set; }
        public required UbicacionDto Ubicacion { get; set; }

        // Nested rather than a flat set of Ubicacion* fields — same shape choice as
        // VehicleDiagnosticDto/ComponentDiagnosticDto's parent/child DTO nesting.
        public class UbicacionDto
        {
            public required string Nombre { get; set; }
            public required string Direccion { get; set; }
            public double Latitud { get; set; }
            public double Longitud { get; set; }
        }
    }
}
