using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class CancelReservationCommand : IRequest
    {
        public required Guid ReservationId { get; set; }
    }
}
