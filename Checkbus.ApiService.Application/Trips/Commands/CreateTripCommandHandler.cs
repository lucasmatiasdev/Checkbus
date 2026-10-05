using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Maps;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public class CreateTripCommandHandler : IRequestHandler<CreateTripCommand, TripDto>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IVehicleDocumentRepository _vehicleDocumentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDriverRequirementRepository _driverRequirementRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IDirectionsService _directionsService;
        private readonly ICurrentUserService _currentUser;

        public CreateTripCommandHandler(
            IVehicleRepository vehicleRepository,
            IVehicleDocumentRepository vehicleDocumentRepository,
            IUserRepository userRepository,
            IDriverRequirementRepository driverRequirementRepository,
            IEventRepository eventRepository,
            ILocationRepository locationRepository,
            ITripRepository tripRepository,
            IDirectionsService directionsService,
            ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _vehicleDocumentRepository = vehicleDocumentRepository;
            _userRepository = userRepository;
            _driverRequirementRepository = driverRequirementRepository;
            _eventRepository = eventRepository;
            _locationRepository = locationRepository;
            _tripRepository = tripRepository;
            _directionsService = directionsService;
            _currentUser = currentUser;
        }

        public async Task<TripDto> Handle(CreateTripCommand request, CancellationToken cancellationToken)
        {
            // Role gate (Planificador/Administrador) is enforced at the controller; this handler
            // only needs to verify tenant ownership of the referenced vehicle/driver, mirroring
            // CreateMaintenanceRecordCommandHandler.
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            // Checkbus.Web's MudDatePicker produces Kind=Unspecified DateTimes; Npgsql's "timestamp
            // with time zone" columns (and any comparison against them, including the overlap
            // queries below) only accept Utc. Normalized once here and reused for the rest of the
            // handler instead of re-converting at every use site.
            var departureDate = DateTime.SpecifyKind(request.DepartureDate, DateTimeKind.Utc);
            var arrivalDate = DateTime.SpecifyKind(request.ArrivalDate, DateTimeKind.Utc);

            var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
            if (vehicle is null || vehicle.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new VehicleNotFoundException();
            }

            await EnsureVehicleEligibleAsync(vehicle, arrivalDate, cancellationToken);
            await EnsureVehicleNoOverlapAsync(request.VehicleId, departureDate, arrivalDate, cancellationToken);

            var driver = await _userRepository.FindByIdAsync(request.DriverId, cancellationToken);
            if (driver is null || driver.OrganizationId != callerOrganizationId || driver.Role != Role.Chofer)
            {
                // IDOR-safe: cross-tenant existence (and a non-Chofer role) is never leaked —
                // someone cannot probe whether a given id belongs to an Administrador this way.
                throw new DriverNotFoundException();
            }

            await EnsureDriverEligibleAsync(driver, arrivalDate, cancellationToken);
            await EnsureDriverNoOverlapAsync(request.DriverId, departureDate, arrivalDate, cancellationToken);

            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event is null)
            {
                throw new EventNotFoundException();
            }

            var resolvedLocations = new List<Location>(request.Stops.Count);
            foreach (var stop in request.Stops)
            {
                var location = await _locationRepository.FindByPlaceIdAsync(stop.PlaceId, cancellationToken);
                if (location is null)
                {
                    var now = DateTime.UtcNow;
                    location = new Location
                    {
                        Id = Guid.NewGuid(),
                        Name = stop.Name,
                        Address = stop.Address,
                        PlaceId = stop.PlaceId,
                        Latitude = stop.Latitude,
                        Longitude = stop.Longitude,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await _locationRepository.AddAsync(location, cancellationToken);
                }

                resolvedLocations.Add(location);
            }

            var points = resolvedLocations
                .Select(l => (l.Latitude, l.Longitude))
                .ToList();

            var directionsResult = await _directionsService.GetDirectionsAsync(points, cancellationToken);

            var createdAt = DateTime.UtcNow;
            var trip = new Trip
            {
                Id = Guid.NewGuid(),
                OrganizationId = callerOrganizationId,
                DriverId = request.DriverId,
                VehicleId = request.VehicleId,
                EventId = request.EventId,
                DepartureDate = departureDate,
                ArrivalDate = arrivalDate,
                Capacity = vehicle.Capacity,
                AvailableSeats = vehicle.Capacity,
                Price = request.Price,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            var route = new Route
            {
                Id = Guid.NewGuid(),
                TripId = trip.Id,
                EstimatedDuration = directionsResult.EstimatedDuration,
                DistanceKm = directionsResult.DistanceKm,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            var stops = new List<Stop>(resolvedLocations.Count);
            for (var i = 0; i < resolvedLocations.Count; i++)
            {
                var type = i == 0
                    ? StopType.Origen
                    : i == resolvedLocations.Count - 1
                        ? StopType.Destino
                        : StopType.Intermedia;

                stops.Add(new Stop
                {
                    Id = Guid.NewGuid(),
                    RouteId = route.Id,
                    LocationId = resolvedLocations[i].Id,
                    Order = i,
                    Type = type,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
            }

            await _tripRepository.AddAsync(trip, route, stops, cancellationToken);

            return new TripDto
            {
                Id = trip.Id,
                VehicleId = trip.VehicleId,
                DriverId = trip.DriverId,
                EventId = trip.EventId,
                DepartureDate = trip.DepartureDate,
                ArrivalDate = trip.ArrivalDate,
                Capacity = trip.Capacity,
                AvailableSeats = trip.AvailableSeats,
                Price = trip.Price,
                Status = trip.Status,
                VehiclePatent = vehicle.Patent,
                VehicleBrand = vehicle.Brand,
                VehicleModel = vehicle.Model,
                DriverName = driver.Name,
                DriverSurname = driver.Surname,
                EventName = @event.Name
            };
        }

        private async Task EnsureVehicleEligibleAsync(Vehicle vehicle, DateTime arrivalDate, CancellationToken cancellationToken)
        {
            // EnMantenimiento deliberately does NOT block — only Inactivo does.
            if (vehicle.Status == VehicleStatus.Inactivo)
            {
                throw new TripNotEligibleException("El vehículo no está habilitado: está inactivo.");
            }

            var documents = await _vehicleDocumentRepository.GetByVehicleIdAsync(vehicle.Id, cancellationToken);
            var arrivalDateOnly = DateOnly.FromDateTime(arrivalDate);

            foreach (var document in documents)
            {
                if (document.Status != VehicleDocumentStatus.Apto)
                {
                    throw new TripNotEligibleException("El vehículo no está habilitado: tiene documentación no apta.");
                }

                // A document with no expiration date set just needs to be Apto — no date check.
                if (document.ExpirationDate is { } expirationDate && expirationDate < arrivalDateOnly)
                {
                    throw new TripNotEligibleException("El vehículo no está habilitado: tiene documentación vencida.");
                }
            }
        }

        private async Task EnsureVehicleNoOverlapAsync(
            Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
        {
            var overlapping = await _tripRepository.GetOverlappingByVehicleIdAsync(vehicleId, departureDate, arrivalDate, cancellationToken);
            if (overlapping.Count > 0)
            {
                throw new TripNotEligibleException("El vehículo ya tiene un viaje asignado en ese rango de fechas.");
            }
        }

        private async Task EnsureDriverEligibleAsync(User driver, DateTime arrivalDate, CancellationToken cancellationToken)
        {
            if (!driver.IsActive)
            {
                throw new TripNotEligibleException("El chofer no está habilitado: la cuenta está inactiva.");
            }

            var requirements = await _driverRequirementRepository.GetByUserIdAsync(driver.Id, cancellationToken);
            var arrivalDateOnly = DateOnly.FromDateTime(arrivalDate);

            foreach (var requirement in requirements)
            {
                if (requirement.Status != DriverRequirementStatus.Apto)
                {
                    throw new TripNotEligibleException("El chofer no está habilitado: tiene documentación no apta.");
                }

                // A requirement with no expiration date set just needs to be Apto — no date check.
                if (requirement.ExpirationDate is { } expirationDate && expirationDate < arrivalDateOnly)
                {
                    throw new TripNotEligibleException("El chofer no está habilitado: tiene documentación vencida.");
                }
            }
        }

        private async Task EnsureDriverNoOverlapAsync(
            Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
        {
            var overlapping = await _tripRepository.GetOverlappingByDriverIdAsync(driverId, departureDate, arrivalDate, cancellationToken);
            if (overlapping.Count > 0)
            {
                throw new TripNotEligibleException("El chofer ya tiene un viaje asignado en ese rango de fechas.");
            }
        }
    }
}
