using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Rutas
{
    // Shared catalog of real-world events — deliberately no OrganizationId, multiple
    // organizations can publish trips to the same event. FK-only relationship to
    // Ubicacion, no navigation property back.
    public class Evento
    {
        public Guid Id { get; set; }
        public required string Nombre { get; set; }
        public EventoTipo Tipo { get; set; }
        public DateTime Fecha { get; set; }
        public required Guid UbicacionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
