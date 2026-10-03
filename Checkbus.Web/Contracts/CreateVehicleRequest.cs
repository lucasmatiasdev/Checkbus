namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Vehicles.Commands.CreateVehicleCommand</c>, the request body
/// for <c>POST /api/Vehicles</c>. Checkbus.Web does not reference the API projects (pure BFF), so
/// this DTO is duplicated here rather than shared. <c>OwnerUserId</c>/<c>OwnerName</c>/
/// <c>OwnerDocumentNumber</c> are conditional on <see cref="OwnerType"/>, enforced server-side by
/// <c>CreateVehicleCommandValidator</c> — no owner field is required when
/// <see cref="WebVehicleOwnerType.Organizacion"/>, <c>OwnerUserId</c> is required when
/// <see cref="WebVehicleOwnerType.Chofer"/>, and <c>OwnerName</c>+<c>OwnerDocumentNumber</c> are
/// required when <see cref="WebVehicleOwnerType.Otro"/>. <see cref="Status"/> and
/// <see cref="OwnerType"/> are serialized as plain integers over the wire, since no
/// <c>JsonStringEnumConverter</c> is registered in this solution.
/// </summary>
public sealed record CreateVehicleRequest
{
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
