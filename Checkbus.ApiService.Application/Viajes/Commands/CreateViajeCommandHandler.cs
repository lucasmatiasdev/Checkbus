using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Maps;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Viajes.Queries;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Rutas;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;
using MediatR;

namespace Checkbus.ApiService.Application.Viajes.Commands
{
    public class CreateViajeCommandHandler : IRequestHandler<CreateViajeCommand, ViajeDto>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IVehicleDocumentRepository _vehicleDocumentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDriverRequirementRepository _driverRequirementRepository;
        private readonly IEventoRepository _eventoRepository;
        private readonly IUbicacionRepository _ubicacionRepository;
        private readonly IViajeRepository _viajeRepository;
        private readonly IDirectionsService _directionsService;
        private readonly ICurrentUserService _currentUser;

        public CreateViajeCommandHandler(
            IVehicleRepository vehicleRepository,
            IVehicleDocumentRepository vehicleDocumentRepository,
            IUserRepository userRepository,
            IDriverRequirementRepository driverRequirementRepository,
            IEventoRepository eventoRepository,
            IUbicacionRepository ubicacionRepository,
            IViajeRepository viajeRepository,
            IDirectionsService directionsService,
            ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _vehicleDocumentRepository = vehicleDocumentRepository;
            _userRepository = userRepository;
            _driverRequirementRepository = driverRequirementRepository;
            _eventoRepository = eventoRepository;
            _ubicacionRepository = ubicacionRepository;
            _viajeRepository = viajeRepository;
            _directionsService = directionsService;
            _currentUser = currentUser;
        }

        public async Task<ViajeDto> Handle(CreateViajeCommand request, CancellationToken cancellationToken)
        {
            // Role gate (Planificador/Administrador) is enforced at the controller; this handler
            // only needs to verify tenant ownership of the referenced vehicle/chofer, mirroring
            // CreateMaintenanceRecordCommandHandler.
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
            if (vehicle is null || vehicle.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new VehicleNotFoundException();
            }

            await EnsureVehicleHabilitadoAsync(vehicle, request.FechaLlegada, cancellationToken);
            await EnsureVehicleNoOverlapAsync(request.VehicleId, request.FechaSalida, request.FechaLlegada, cancellationToken);

            var chofer = await _userRepository.FindByIdAsync(request.ChoferId, cancellationToken);
            if (chofer is null || chofer.OrganizationId != callerOrganizationId || chofer.Role != Role.Chofer)
            {
                // IDOR-safe: cross-tenant existence (and a non-Chofer role) is never leaked —
                // someone cannot probe whether a given id belongs to an Administrador this way.
                throw new ChoferNotFoundException();
            }

            await EnsureChoferHabilitadoAsync(chofer, request.FechaLlegada, cancellationToken);
            await EnsureChoferNoOverlapAsync(request.ChoferId, request.FechaSalida, request.FechaLlegada, cancellationToken);

            var evento = await _eventoRepository.GetByIdAsync(request.EventoId, cancellationToken);
            if (evento is null)
            {
                throw new EventoNotFoundException();
            }

            var resolvedUbicaciones = new List<Ubicacion>(request.Stops.Count);
            foreach (var stop in request.Stops)
            {
                var ubicacion = await _ubicacionRepository.FindByPlaceIdAsync(stop.PlaceId, cancellationToken);
                if (ubicacion is null)
                {
                    var now = DateTime.UtcNow;
                    ubicacion = new Ubicacion
                    {
                        Id = Guid.NewGuid(),
                        Nombre = stop.Nombre,
                        Direccion = stop.Direccion,
                        PlaceId = stop.PlaceId,
                        Latitud = stop.Latitud,
                        Longitud = stop.Longitud,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await _ubicacionRepository.AddAsync(ubicacion, cancellationToken);
                }

                resolvedUbicaciones.Add(ubicacion);
            }

            var points = resolvedUbicaciones
                .Select(u => (u.Latitud, u.Longitud))
                .ToList();

            var directionsResult = await _directionsService.GetDirectionsAsync(points, cancellationToken);

            var createdAt = DateTime.UtcNow;
            var viaje = new Viaje
            {
                Id = Guid.NewGuid(),
                OrganizationId = callerOrganizationId,
                ChoferId = request.ChoferId,
                VehicleId = request.VehicleId,
                EventoId = request.EventoId,
                FechaSalida = request.FechaSalida,
                FechaLlegada = request.FechaLlegada,
                Capacidad = vehicle.Capacity,
                AsientosDisponibles = vehicle.Capacity,
                Precio = request.Precio,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            var ruta = new Ruta
            {
                Id = Guid.NewGuid(),
                ViajeId = viaje.Id,
                TiempoEstimado = directionsResult.TiempoEstimado,
                DistanciaKm = directionsResult.DistanciaKm,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            var stops = new List<Stop>(resolvedUbicaciones.Count);
            for (var i = 0; i < resolvedUbicaciones.Count; i++)
            {
                var tipo = i == 0
                    ? StopTipo.Origen
                    : i == resolvedUbicaciones.Count - 1
                        ? StopTipo.Destino
                        : StopTipo.Intermedia;

                stops.Add(new Stop
                {
                    Id = Guid.NewGuid(),
                    RutaId = ruta.Id,
                    UbicacionId = resolvedUbicaciones[i].Id,
                    Orden = i,
                    Tipo = tipo,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
            }

            await _viajeRepository.AddAsync(viaje, ruta, stops, cancellationToken);

            return new ViajeDto
            {
                Id = viaje.Id,
                VehicleId = viaje.VehicleId,
                ChoferId = viaje.ChoferId,
                EventoId = viaje.EventoId,
                FechaSalida = viaje.FechaSalida,
                FechaLlegada = viaje.FechaLlegada,
                Capacidad = viaje.Capacidad,
                AsientosDisponibles = viaje.AsientosDisponibles,
                Precio = viaje.Precio,
                Estado = viaje.Estado,
                VehiclePatent = vehicle.Patent,
                VehicleBrand = vehicle.Brand,
                VehicleModel = vehicle.Model,
                ChoferName = chofer.Name,
                ChoferSurname = chofer.Surname,
                EventoNombre = evento.Nombre
            };
        }

        private async Task EnsureVehicleHabilitadoAsync(Vehicle vehicle, DateTime fechaLlegada, CancellationToken cancellationToken)
        {
            // EnMantenimiento deliberately does NOT block — only Inactivo does.
            if (vehicle.Status == VehicleStatus.Inactivo)
            {
                throw new ViajeNotHabilitadoException("El vehículo no está habilitado: está inactivo.");
            }

            var documents = await _vehicleDocumentRepository.GetByVehicleIdAsync(vehicle.Id, cancellationToken);
            var fechaLlegadaDate = DateOnly.FromDateTime(fechaLlegada);

            foreach (var document in documents)
            {
                if (document.Status != VehicleDocumentStatus.Apto)
                {
                    throw new ViajeNotHabilitadoException("El vehículo no está habilitado: tiene documentación no apta.");
                }

                // A document with no expiration date set just needs to be Apto — no date check.
                if (document.ExpirationDate is { } expirationDate && expirationDate < fechaLlegadaDate)
                {
                    throw new ViajeNotHabilitadoException("El vehículo no está habilitado: tiene documentación vencida.");
                }
            }
        }

        private async Task EnsureVehicleNoOverlapAsync(
            Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
        {
            var overlapping = await _viajeRepository.GetOverlappingByVehicleIdAsync(vehicleId, fechaSalida, fechaLlegada, cancellationToken);
            if (overlapping.Count > 0)
            {
                throw new ViajeNotHabilitadoException("El vehículo ya tiene un viaje asignado en ese rango de fechas.");
            }
        }

        private async Task EnsureChoferHabilitadoAsync(User chofer, DateTime fechaLlegada, CancellationToken cancellationToken)
        {
            if (!chofer.IsActive)
            {
                throw new ViajeNotHabilitadoException("El chofer no está habilitado: la cuenta está inactiva.");
            }

            var requirements = await _driverRequirementRepository.GetByUserIdAsync(chofer.Id, cancellationToken);
            var fechaLlegadaDate = DateOnly.FromDateTime(fechaLlegada);

            foreach (var requirement in requirements)
            {
                if (requirement.Status != DriverRequirementStatus.Apto)
                {
                    throw new ViajeNotHabilitadoException("El chofer no está habilitado: tiene documentación no apta.");
                }

                // A requirement with no expiration date set just needs to be Apto — no date check.
                if (requirement.ExpirationDate is { } expirationDate && expirationDate < fechaLlegadaDate)
                {
                    throw new ViajeNotHabilitadoException("El chofer no está habilitado: tiene documentación vencida.");
                }
            }
        }

        private async Task EnsureChoferNoOverlapAsync(
            Guid choferId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
        {
            var overlapping = await _viajeRepository.GetOverlappingByChoferIdAsync(choferId, fechaSalida, fechaLlegada, cancellationToken);
            if (overlapping.Count > 0)
            {
                throw new ViajeNotHabilitadoException("El chofer ya tiene un viaje asignado en ese rango de fechas.");
            }
        }
    }
}
