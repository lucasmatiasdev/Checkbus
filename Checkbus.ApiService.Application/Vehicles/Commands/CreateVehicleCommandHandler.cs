using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Vehicles;
using MediatR;

namespace Checkbus.ApiService.Application.Vehicles.Commands
{
    public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, CreateVehicleCommandResult>
    {

        private static readonly VehicleDocumentType[] UniversalDocumentTypes =
        [
            VehicleDocumentType.Seguro,
            VehicleDocumentType.RTO_VTV
        ];

        private readonly IVehicleRepository _vehicleRepository;
        private readonly IVehicleDocumentRepository _vehicleDocumentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUser;

        public CreateVehicleCommandHandler(
            IVehicleRepository vehicleRepository,
            IVehicleDocumentRepository vehicleDocumentRepository,
            IUserRepository userRepository,
            ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _vehicleDocumentRepository = vehicleDocumentRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        public async Task<CreateVehicleCommandResult> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
        {

            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            if (request.OwnerType == VehicleOwnerType.Chofer)
            {
                var owner = await _userRepository.FindByIdAsync(request.OwnerUserId!.Value, cancellationToken);

                if (owner is null || owner.Role != Role.Chofer || owner.OrganizationId != organizationId)
                {
                    throw new VehicleOwnerNotFoundException();
                }
            }

            if (await _vehicleRepository.PatentExistsAsync(request.Patent, organizationId, cancellationToken))
            {
                throw new PatentAlreadyRegisteredException();
            }

            var now = DateTime.UtcNow;
            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Brand = request.Brand,
                Model = request.Model,
                Year = request.Year,
                Patent = request.Patent,
                Capacity = request.Capacity,
                Mileage = request.Mileage,
                Status = request.Status,
                OrganizationId = organizationId,
                OwnerType = request.OwnerType,
                OwnerUserId = request.OwnerType == VehicleOwnerType.Chofer ? request.OwnerUserId : null,
                OwnerName = request.OwnerType == VehicleOwnerType.Otro ? request.OwnerName : null,
                OwnerDocumentNumber = request.OwnerType == VehicleOwnerType.Otro ? request.OwnerDocumentNumber : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _vehicleRepository.AddAsync(vehicle, cancellationToken);

            var documentsNow = DateTime.UtcNow;
            var documents = UniversalDocumentTypes.Select(type => new VehicleDocument
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicle.Id,
                Type = type,
                Status = VehicleDocumentStatus.Pendiente,
                DocumentPresent = false,
                CreatedAt = documentsNow,
                UpdatedAt = documentsNow
            });

            await _vehicleDocumentRepository.AddRangeAsync(documents, cancellationToken);

            return new CreateVehicleCommandResult
            {
                VehicleId = vehicle.Id,
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
                OwnerDocumentNumber = vehicle.OwnerDocumentNumber,
                OrganizationId = vehicle.OrganizationId
            };
        }
    }
}
