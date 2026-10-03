using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using MediatR;

namespace Checkbus.ApiService.Application.Vehicles.Queries
{
    public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, IReadOnlyList<VehicleListItemDto>>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ICurrentUserService _currentUser;

        public GetVehiclesQueryHandler(IVehicleRepository vehicleRepository, ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<VehicleListItemDto>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim — the query has no
            // OrganizationId field, so no request value can ever influence it.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var vehicles = await _vehicleRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);

            return vehicles
                .Select(vehicle => new VehicleListItemDto
                {
                    Id = vehicle.Id,
                    Brand = vehicle.Brand,
                    Model = vehicle.Model,
                    Year = vehicle.Year,
                    Patent = vehicle.Patent,
                    Capacity = vehicle.Capacity,
                    Mileage = vehicle.Mileage,
                    Status = vehicle.Status,
                    OwnerType = vehicle.OwnerType,
                    OwnerUserId = vehicle.OwnerUserId,
                    OwnerName = vehicle.OwnerName,
                    OwnerDocumentNumber = vehicle.OwnerDocumentNumber
                })
                .ToList();
        }
    }
}
