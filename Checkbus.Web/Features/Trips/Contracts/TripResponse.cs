namespace Checkbus.Web.Features.Trips.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Application.Trips.Queries.TripDto</c>, the
/// response body shape for <c>POST /api/Trips</c> (201) and one item of the <c>200</c> array
/// for <c>GET /api/Trips</c>. Checkbus.Web does not reference the API projects (pure BFF), so
/// this DTO is duplicated here rather than shared.
/// </summary>
public sealed record TripResponse
{
    public required Guid Id { get; init; }
    public required Guid VehicleId { get; init; }
    public required Guid DriverId { get; init; }
    public required Guid EventId { get; init; }
    public DateTime DepartureDate { get; init; }
    public DateTime ArrivalDate { get; init; }
    public int Capacity { get; init; }
    public int AvailableSeats { get; init; }
    public decimal Price { get; init; }
    public required WebTripStatus Status { get; init; }

    public string? VehiclePatent { get; init; }
    public string? VehicleBrand { get; init; }
    public string? VehicleModel { get; init; }
    public string? DriverName { get; init; }
    public string? DriverSurname { get; init; }
    public string? EventName { get; init; }
}
