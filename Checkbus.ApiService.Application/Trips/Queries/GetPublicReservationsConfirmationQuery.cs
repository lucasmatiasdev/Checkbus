using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetPublicReservationsConfirmationQuery : IRequest<PublicReservationsConfirmationDto>
    {
        public required IReadOnlyList<Guid> ReservationIds { get; set; }
    }
}
