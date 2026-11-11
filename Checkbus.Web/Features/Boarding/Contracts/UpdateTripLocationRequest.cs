namespace Checkbus.Web.Features.Boarding.Contracts;

/// <summary>
/// DEMO-ONLY (odd/tasks/seguimiento-tiempo-real-demo.md) — web-side mirror of
/// <c>Checkbus.ApiService.Contracts.UpdateTripLocationRequest</c>, the request body for
/// <c>POST /api/Boarding/{tripId}/location</c>.
/// </summary>
public sealed record UpdateTripLocationRequest(double Latitude, double Longitude);
