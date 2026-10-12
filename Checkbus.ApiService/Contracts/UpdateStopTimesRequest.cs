namespace Checkbus.ApiService.Contracts
{
    public sealed record UpdateStopTimesRequest(IReadOnlyList<StopDepartureTimeRequest> Stops);

    public sealed record StopDepartureTimeRequest(Guid StopId, DateTime DepartureTime);
}
