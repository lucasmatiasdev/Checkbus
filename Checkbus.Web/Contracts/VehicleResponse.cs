namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Vehicles.Queries.VehicleListItemDto</c>, one item of the
/// <c>200</c> response body array for <c>GET /api/Vehicles</c>. Checkbus.Web does not reference the
/// API projects (pure BFF), so this DTO is duplicated here rather than shared.
/// </summary>
public sealed record VehicleResponse
{
    public required Guid Id { get; init; }
    public required string Brand { get; init; }
    public required string Model { get; init; }
    public required int Year { get; init; }
    public required string Patent { get; init; }
    public required int Capacity { get; init; }
    public required int Mileage { get; init; }
    public required WebVehicleStatus Status { get; init; }
    public required WebVehicleOwnerType OwnerType { get; init; }
    public Guid? OwnerUserId { get; init; }
    public string? OwnerName { get; init; }
    public string? OwnerDocumentNumber { get; init; }
}
