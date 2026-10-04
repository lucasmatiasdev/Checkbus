namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Eventos.Commands.CreateEventoCommand</c>, the request body
/// for <c>POST /api/Eventos</c>. The location fields are flattened to match
/// <c>Checkbus.Web.Models.PickedLocation</c>'s shape exactly, so a page can map one straight into
/// the other with no renaming (<c>EventoNew.razor</c> does this with R3's <c>LocationPicker</c>
/// in <c>LocationPickerMode.Single</c> mode).
/// </summary>
public sealed record CreateEventoRequest
{
    public required string Nombre { get; init; }
    public required WebEventoTipo Tipo { get; init; }
    public required DateTime Fecha { get; init; }
    public required string UbicacionNombre { get; init; }
    public required string Direccion { get; init; }
    public required string PlaceId { get; init; }
    public required double Latitud { get; init; }
    public required double Longitud { get; init; }
}
