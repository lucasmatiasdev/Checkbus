using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Rutas
{
    // A Ubicacion's role within one specific Ruta. FK-only, no navigation properties back
    // to Ruta/Ubicacion — same pattern as VehicleDocument -> Vehicle. No independent
    // lifecycle or endpoint: always created together with its parent Viaje/Ruta.
    public class Stop
    {
        public Guid Id { get; set; }
        public required Guid RutaId { get; set; }
        public required Guid UbicacionId { get; set; }
        public int Orden { get; set; }
        public StopTipo Tipo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
