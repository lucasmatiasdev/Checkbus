using MediatR;

namespace Checkbus.ApiService.Application.Vehicles.Queries
{
    public class GetVehiclesQuery : IRequest<IReadOnlyList<VehicleListItemDto>>
    {

    }
}
