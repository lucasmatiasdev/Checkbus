using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Contracts
{
    public sealed record ValidateBoardingRequest(Guid ReservationId, ValidationMethod Method);
}
