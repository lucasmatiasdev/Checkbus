namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Application.Viajes.Queries.ViajeDto</c>, the
/// response body shape for <c>POST /api/Viajes</c> (201) and one item of the <c>200</c> array
/// for <c>GET /api/Viajes</c>. Checkbus.Web does not reference the API projects (pure BFF), so
/// this DTO is duplicated here rather than shared.
/// </summary>
public sealed record ViajeResponse
{
    public required Guid Id { get; init; }
    public required Guid VehicleId { get; init; }
    public required Guid ChoferId { get; init; }
    public required Guid EventoId { get; init; }
    public DateTime FechaSalida { get; init; }
    public DateTime FechaLlegada { get; init; }
    public int Capacidad { get; init; }
    public int AsientosDisponibles { get; init; }
    public decimal Precio { get; init; }
    public required WebViajeEstado Estado { get; init; }

    public string? VehiclePatent { get; init; }
    public string? VehicleBrand { get; init; }
    public string? VehicleModel { get; init; }
    public string? ChoferName { get; init; }
    public string? ChoferSurname { get; init; }
    public string? EventoNombre { get; init; }
}

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Viajes.Queries.ViajeDetailDto</c>, the response body shape
/// for <c>GET /api/Viajes/{id}</c> — same display fields as <see cref="ViajeResponse"/> plus the
/// nested <see cref="Ruta"/>/stops detail.
/// </summary>
public sealed record ViajeDetailResponse
{
    public required Guid Id { get; init; }
    public required Guid VehicleId { get; init; }
    public required Guid ChoferId { get; init; }
    public required Guid EventoId { get; init; }
    public DateTime FechaSalida { get; init; }
    public DateTime FechaLlegada { get; init; }
    public int Capacidad { get; init; }
    public int AsientosDisponibles { get; init; }
    public decimal Precio { get; init; }
    public required WebViajeEstado Estado { get; init; }

    public string? VehiclePatent { get; init; }
    public string? VehicleBrand { get; init; }
    public string? VehicleModel { get; init; }
    public string? ChoferName { get; init; }
    public string? ChoferSurname { get; init; }
    public string? EventoNombre { get; init; }

    public required RutaResponse Ruta { get; init; }
}

/// <summary>One <see cref="ViajeDetailResponse.Ruta"/> — its computed estimates plus ordered stops.</summary>
public sealed record RutaResponse
{
    public TimeSpan TiempoEstimado { get; init; }
    public decimal DistanciaKm { get; init; }
    public required IReadOnlyList<StopResponse> Stops { get; init; }
}

/// <summary>One ordered stop of <see cref="RutaResponse.Stops"/>, with its resolved Ubicacion.</summary>
public sealed record StopResponse
{
    public int Orden { get; init; }
    public required WebStopTipo Tipo { get; init; }
    public required string Nombre { get; init; }
    public required string Direccion { get; init; }
    public double Latitud { get; init; }
    public double Longitud { get; init; }
}
