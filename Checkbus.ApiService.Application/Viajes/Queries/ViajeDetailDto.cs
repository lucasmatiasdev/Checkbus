using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Viajes.Queries
{
    // Same display fields as ViajeDto, plus the nested Ruta/Stops detail — kept as a separate
    // type (rather than reusing ViajeDto) because GetViajesQueryHandler (list) deliberately never
    // resolves Ruta/Stops/Ubicacion per item to avoid an N+1 fan-out across the whole list.
    public class ViajeDetailDto
    {
        public required Guid Id { get; set; }
        public required Guid VehicleId { get; set; }
        public required Guid ChoferId { get; set; }
        public required Guid EventoId { get; set; }
        public DateTime FechaSalida { get; set; }
        public DateTime FechaLlegada { get; set; }
        public int Capacidad { get; set; }
        public int AsientosDisponibles { get; set; }
        public decimal Precio { get; set; }
        public ViajeEstado Estado { get; set; }

        public string? VehiclePatent { get; set; }
        public string? VehicleBrand { get; set; }
        public string? VehicleModel { get; set; }
        public string? ChoferName { get; set; }
        public string? ChoferSurname { get; set; }
        public string? EventoNombre { get; set; }

        public required RutaDto Ruta { get; set; }

        public class RutaDto
        {
            public TimeSpan TiempoEstimado { get; set; }
            public decimal DistanciaKm { get; set; }
            public required IReadOnlyList<StopDto> Stops { get; set; }
        }

        public class StopDto
        {
            public int Orden { get; set; }
            public StopTipo Tipo { get; set; }
            public required string Nombre { get; set; }
            public required string Direccion { get; set; }
            public double Latitud { get; set; }
            public double Longitud { get; set; }
        }
    }
}
