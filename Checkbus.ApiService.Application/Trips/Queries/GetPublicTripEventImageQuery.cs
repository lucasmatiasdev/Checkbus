using MediatR;
using Checkbus.ApiService.Application.Events.Queries;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetPublicTripEventImageQuery : IRequest<EventImageFileDto>
    {
        public required Guid TripId { get; set; }
    }
}
