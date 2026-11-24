using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetDriverTripsQuery : IRequest<IReadOnlyList<TripDto>>
    {
    }
}
