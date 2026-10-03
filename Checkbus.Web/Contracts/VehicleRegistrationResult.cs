namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Vehicles.Commands.CreateVehicleCommandResult</c>, the
/// <c>201</c> response body for <c>POST /api/Vehicles</c>. Checkbus.Web does not reference the API
/// projects (pure BFF), so this DTO is duplicated here rather than shared — matching the existing
/// <c>RegisterUserResult</c> mirror-DTO convention (a dedicated creation-result type, distinct from
/// the list item type, because the two wire shapes genuinely differ: the create result's id field is
/// named <c>VehicleId</c> rather than <c>Id</c> and it additionally carries
/// <c>OrganizationId</c>). This file was not in V4's originally enumerated allowed edit surface;
/// it was added because binding the create response onto <see cref="VehicleResponse"/> instead
/// would silently deserialize <c>Id</c> as <see cref="Guid.Empty"/> (property name mismatch), the
/// same kind of gap V2 flagged for <c>VehicleExceptionHandler</c> registration.
/// </summary>
public sealed record VehicleRegistrationResult
{
    public required Guid VehicleId { get; init; }
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
    public required Guid OrganizationId { get; init; }
}
