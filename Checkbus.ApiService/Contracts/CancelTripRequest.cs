using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Contracts
{
    public sealed record CancelTripRequest(TripCancellationReason Reason, string? Description);
}
