namespace Checkbus.Web.Features.Boarding.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Contracts.ValidateBoardingRequest</c> — the request
/// body for <c>POST /api/boarding/validate</c>.
/// </summary>
public sealed record ValidateBoardingRequest(Guid ReservationId, WebValidationMethod Method);
