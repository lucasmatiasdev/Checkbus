namespace Checkbus.Web.Features.Trips.Contracts;

/// <summary>
/// Web-side mirror of the API's <c>PendingRefundDto</c>, one entry per Payment whose refund
/// never reached the gateway after a trip cancellation (CU-05).
/// </summary>
public sealed record PendingRefundResponse
{
    public required Guid PaymentId { get; init; }
    public string? ExternalOrderId { get; init; }
    public required decimal Amount { get; init; }
    public required IReadOnlyList<Guid> ReservationIds { get; init; }
}
