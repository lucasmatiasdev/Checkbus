using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class ValidateBoardingCommand : IRequest
    {
        public required Guid ReservationId { get; set; }
        public required ValidationMethod Method { get; set; }
    }
}
