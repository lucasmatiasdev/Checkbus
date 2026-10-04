using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Interfaces;

namespace Checkbus.ApiService.Domain.Entities.Rutas
{
    // The only entity in this module that carries OrganizationId: Ubicacion/Evento are
    // shared catalogs, Ruta/Stop are scoped transitively through this Viaje. ChoferId,
    // VehicleId and EventoId are FK-only, no navigation property back, same pattern as
    // VehicleDocument -> Vehicle. Capacidad is a snapshot of Vehicle.Capacity taken at
    // creation time so a later change to the vehicle never affects already-published trips.
    public class Viaje : ITenantEntity
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public required Guid ChoferId { get; set; }
        public required Guid VehicleId { get; set; }
        public required Guid EventoId { get; set; }
        public DateTime FechaSalida { get; set; }
        public DateTime FechaLlegada { get; set; }
        public int Capacidad { get; set; }
        public int AsientosDisponibles { get; set; }
        public decimal Precio { get; set; }
        public ViajeEstado Estado { get; set; } = ViajeEstado.Programado;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
