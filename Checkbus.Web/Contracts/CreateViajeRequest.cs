namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Viajes.Commands.CreateViajeCommand</c>, the request body
/// for <c>POST /api/Viajes</c>. Each stop's location fields are flattened to match
/// <c>Checkbus.Web.Models.PickedLocation</c>'s shape exactly (same convention as
/// <c>CreateEventoRequest</c>), so <c>ViajeNew.razor</c> can map R3's <c>LocationPicker</c>
/// <c>MultiStop</c> output straight into the ordered <see cref="Stops"/> list with no renaming.
/// </summary>
public sealed record CreateViajeRequest
{
    public required Guid VehicleId { get; init; }
    public required Guid ChoferId { get; init; }
    public required Guid EventoId { get; init; }
    public required DateTime FechaSalida { get; init; }
    public required DateTime FechaLlegada { get; init; }
    public required decimal Precio { get; init; }
    public required IReadOnlyList<CreateViajeStopRequest> Stops { get; init; }
}

/// <summary>
/// One ordered stop of <see cref="CreateViajeRequest.Stops"/>. Orden/Tipo are deliberately
/// absent — both are computed server-side from this list's position.
/// </summary>
public sealed record CreateViajeStopRequest
{
    public required string Nombre { get; init; }
    public required string Direccion { get; init; }
    public required string PlaceId { get; init; }
    public required double Latitud { get; init; }
    public required double Longitud { get; init; }
}
