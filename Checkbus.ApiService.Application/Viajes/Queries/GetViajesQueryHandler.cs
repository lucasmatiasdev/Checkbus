using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using MediatR;

namespace Checkbus.ApiService.Application.Viajes.Queries
{
    public class GetViajesQueryHandler : IRequestHandler<GetViajesQuery, IReadOnlyList<ViajeDto>>
    {
        private readonly IViajeRepository _viajeRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEventoRepository _eventoRepository;
        private readonly ICurrentUserService _currentUser;

        public GetViajesQueryHandler(
            IViajeRepository viajeRepository,
            IVehicleRepository vehicleRepository,
            IUserRepository userRepository,
            IEventoRepository eventoRepository,
            ICurrentUserService currentUser)
        {
            _viajeRepository = viajeRepository;
            _vehicleRepository = vehicleRepository;
            _userRepository = userRepository;
            _eventoRepository = eventoRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<ViajeDto>> Handle(GetViajesQuery request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim — mirrors GetVehiclesQueryHandler.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var viajes = await _viajeRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);

            // Vehicles/choferes referenced by this org's viajes are always themselves in this org
            // (enforced at creation time), so a single org-scoped fetch each covers every join.
            var vehicles = await _vehicleRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);
            var vehiclesById = vehicles.ToDictionary(v => v.Id);

            var users = await _userRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);
            var usersById = users.ToDictionary(u => u.Id);

            // Evento is a shared catalog with no batch-by-ids lookup, so resolve each distinct
            // Evento at most once — mirrors GetEventosQueryHandler's Ubicacion resolution.
            var eventosById = new Dictionary<Guid, Evento>();
            foreach (var eventoId in viajes.Select(v => v.EventoId).Distinct())
            {
                var evento = await _eventoRepository.GetByIdAsync(eventoId, cancellationToken);
                if (evento is not null)
                {
                    eventosById[eventoId] = evento;
                }
            }

            return viajes.Select(v => MapToDto(v, vehiclesById, usersById, eventosById)).ToList();
        }

        private static ViajeDto MapToDto(
            Domain.Entities.Rutas.Viaje viaje,
            Dictionary<Guid, Vehicle> vehiclesById,
            Dictionary<Guid, User> usersById,
            Dictionary<Guid, Evento> eventosById)
        {
            vehiclesById.TryGetValue(viaje.VehicleId, out var vehicle);
            usersById.TryGetValue(viaje.ChoferId, out var chofer);
            eventosById.TryGetValue(viaje.EventoId, out var evento);

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
                VehiclePatent = vehicle?.Patent,
                VehicleBrand = vehicle?.Brand,
                VehicleModel = vehicle?.Model,
                ChoferName = chofer?.Name,
                ChoferSurname = chofer?.Surname,
                EventoNombre = evento?.Nombre
            };
        }
    }
}
