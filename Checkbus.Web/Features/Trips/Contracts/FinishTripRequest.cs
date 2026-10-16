namespace Checkbus.Web.Features.Trips.Contracts;

/// <summary>
/// Web-side mirror of the API's <c>Checkbus.ApiService.Contracts.FinishTripRequest</c>, the
/// request body for <c>POST api/Trips/{id}/finish</c> (CU-08).
/// </summary>
public sealed record FinishTripRequest
{
    public required DateTime ActualArrivalDate { get; init; }
    public string? Observations { get; init; }
}
