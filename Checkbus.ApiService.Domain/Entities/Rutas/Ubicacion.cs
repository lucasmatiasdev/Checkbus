namespace Checkbus.ApiService.Domain.Entities.Rutas
{
    // Shared catalog of real-world places — deliberately no OrganizationId, the same
    // physical place can be reused across organizations/routes/events. PlaceId is
    // Google's place id and is used to dedupe: callers must look up by PlaceId before
    // inserting a new row (see IUbicacionRepository.FindByPlaceIdAsync).
    public class Ubicacion
    {
        public Guid Id { get; set; }
        public required string Nombre { get; set; }
        public required string Direccion { get; set; }
        public required string PlaceId { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
