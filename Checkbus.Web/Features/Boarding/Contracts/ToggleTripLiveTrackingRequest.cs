namespace Checkbus.Web.Features.Boarding.Contracts;

/// <summary>
/// DEMO-ONLY (odd/tasks/seguimiento-tiempo-real-demo.md) — web-side mirror of
/// <c>Checkbus.ApiService.Contracts.ToggleTripLiveTrackingRequest</c>, the request body for
/// <c>POST /api/Boarding/{tripId}/location/toggle</c>.
/// </summary>
public sealed record ToggleTripLiveTrackingRequest(bool Enabled);
