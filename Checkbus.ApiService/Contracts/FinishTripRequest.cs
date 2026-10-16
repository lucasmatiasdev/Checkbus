namespace Checkbus.ApiService.Contracts
{
    public sealed record FinishTripRequest(DateTime ActualArrivalDate, string? Observations);
}
