namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Application.Eventos.Queries.EventoDto</c>, the
/// response body shape for <c>POST /api/Eventos</c> (201) and one item of the <c>200</c> array
/// for <c>GET /api/Eventos</c>. Checkbus.Web does not reference the API projects (pure BFF), so
/// this DTO is duplicated here rather than shared.
/// </summary>
public sealed record EventoResponse
{
    public required Guid Id { get; init; }
    public required string Nombre { get; init; }
    public required WebEventoTipo Tipo { get; init; }
    public DateTime Fecha { get; init; }
    public required UbicacionResponse Ubicacion { get; init; }
}

/// <summary>
/// One item of <see cref="EventoResponse.Ubicacion"/> — nested rather than flattened, same shape
/// choice as <see cref="VehicleDiagnosticResponse"/>/<see cref="ComponentDiagnosticResponse"/>.
/// </summary>
public sealed record UbicacionResponse
{
    public required string Nombre { get; init; }
    public required string Direccion { get; init; }
    public double Latitud { get; init; }
    public double Longitud { get; init; }
}
