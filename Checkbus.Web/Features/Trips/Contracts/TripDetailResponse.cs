namespace Checkbus.Web.Features.Trips.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Queries.TripDetailDto</c>, the response body shape
/// for <c>GET /api/Trips/{id}</c> — same display fields as <see cref="TripResponse"/> plus the
/// nested <see cref="Route"/>/stops detail.
/// </summary>
public sealed record TripDetailResponse
{
    public required Guid Id { get; init; }
    public required Guid VehicleId { get; init; }
    public required Guid DriverId { get; init; }
    public required Guid EventId { get; init; }
    public required string Slug { get; init; }
    public DateTime DepartureDate { get; init; }
    public DateTime ArrivalDate { get; init; }
    public int Capacity { get; init; }
    public int AvailableSeats { get; init; }
    public decimal Price { get; init; }
    public required WebTripStatus Status { get; init; }
    public DateTime? ActualArrivalDate { get; init; }
    public string? Observations { get; init; }

    public string? VehiclePatent { get; init; }
    public string? VehicleBrand { get; init; }
    public string? VehicleModel { get; init; }
    public string? DriverName { get; init; }
    public string? DriverSurname { get; init; }
    public string? EventName { get; init; }

    public required RouteResponse Route { get; init; }
}
