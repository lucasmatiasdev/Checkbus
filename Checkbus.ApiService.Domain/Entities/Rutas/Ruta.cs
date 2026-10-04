namespace Checkbus.ApiService.Domain.Entities.Rutas
{
    // Composition, not reusable: a Ruta never exists without exactly one Viaje and is
    // never shared. ViajeId is unique, mirroring the existing child-holds-FK-to-parent
    // convention (FK lives on the child, no navigation property back).
    // TiempoEstimado/DistanciaKm are calculated once, server-side, at Viaje creation time
    // via the Directions API — never recalculated on read, never trusted from the client.
    public class Ruta
    {
        public Guid Id { get; set; }
        public required Guid ViajeId { get; set; }
        public TimeSpan TiempoEstimado { get; set; }
        public decimal DistanciaKm { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
