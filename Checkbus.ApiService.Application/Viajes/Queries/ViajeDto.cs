using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Viajes.Queries
{
    public class ViajeDto
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

        // Display-only join fields — same shape choice as MaintenanceRecordDto's
        // VehiclePatent/Brand/Model — avoids a second round trip to render the Viajes list/detail.
        public string? VehiclePatent { get; set; }
        public string? VehicleBrand { get; set; }
        public string? VehicleModel { get; set; }
        public string? ChoferName { get; set; }
        public string? ChoferSurname { get; set; }
        public string? EventoNombre { get; set; }
    }
}
