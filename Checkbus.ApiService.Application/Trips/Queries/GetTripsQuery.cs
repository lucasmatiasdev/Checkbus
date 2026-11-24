using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetTripsQuery : IRequest<IReadOnlyList<TripDto>>
    {
    }
}
