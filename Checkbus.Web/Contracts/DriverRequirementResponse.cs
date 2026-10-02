namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.DriverRequirements.Queries.DriverRequirementDto</c>, one
/// item of the <c>200</c> response body array for
/// <c>GET /api/DriverRequirements/{targetUserId}</c>. Checkbus.Web does not reference the API
/// projects (pure BFF), so this DTO is duplicated here rather than shared. Like the real DTO, this
/// deliberately excludes the file storage key: the uploaded file is only ever reachable through
/// the dedicated download endpoint, never as a raw storage key handed to the client.
/// </summary>
public sealed record DriverRequirementResponse
{
    public required WebDriverRequirementType Type { get; init; }
    public required WebDriverRequirementStatus Status { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? ExpirationDate { get; init; }
    public bool DocumentPresent { get; init; }
}
