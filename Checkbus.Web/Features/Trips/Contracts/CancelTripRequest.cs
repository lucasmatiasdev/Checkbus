namespace Checkbus.Web.Features.Trips.Contracts;

/// <summary>
/// Web-side mirror of the API's <c>Checkbus.ApiService.Contracts.CancelTripRequest</c>, the
/// request body for <c>POST api/Trips/{id}/cancel</c> (CU-05).
/// </summary>
public sealed record CancelTripRequest
{
    public required WebTripCancellationReason Reason { get; init; }
    public string? Description { get; init; }
}
