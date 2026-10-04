using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Rutas;
using MediatR;

namespace Checkbus.ApiService.Application.Viajes.Queries
{
    public class GetViajeQueryHandler : IRequestHandler<GetViajeQuery, ViajeDetailDto>
    {
        private readonly IViajeRepository _viajeRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEventoRepository _eventoRepository;
        private readonly IUbicacionRepository _ubicacionRepository;
        private readonly ICurrentUserService _currentUser;

        public GetViajeQueryHandler(
            IViajeRepository viajeRepository,
            IVehicleRepository vehicleRepository,
            IUserRepository userRepository,
            IEventoRepository eventoRepository,
            IUbicacionRepository ubicacionRepository,
            ICurrentUserService currentUser)
        {
            _viajeRepository = viajeRepository;
            _vehicleRepository = vehicleRepository;
            _userRepository = userRepository;
            _eventoRepository = eventoRepository;
            _ubicacionRepository = ubicacionRepository;
            _currentUser = currentUser;
        }

        public async Task<ViajeDetailDto> Handle(GetViajeQuery request, CancellationToken cancellationToken)
        {
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var viaje = await _viajeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (viaje is null || viaje.OrganizationId != organizationId)
            {
                // IDOR-safe: a viaje belonging to another organization looks identical to a
                // missing one — cross-tenant existence is never leaked to the caller.
                throw new ViajeNotFoundException();
            }

            var vehicle = await _vehicleRepository.GetByIdAsync(viaje.VehicleId, cancellationToken);
            var chofer = await _userRepository.FindByIdAsync(viaje.ChoferId, cancellationToken);
            var evento = await _eventoRepository.GetByIdAsync(viaje.EventoId, cancellationToken);

            var ruta = await _viajeRepository.GetRutaByViajeIdAsync(viaje.Id, cancellationToken);
            var stopDtos = new List<ViajeDetailDto.StopDto>();

            if (ruta is not null)
            {
                var stops = await _viajeRepository.GetStopsByRutaIdAsync(ruta.Id, cancellationToken);

                foreach (var stop in stops)
                {
                    var ubicacion = await _ubicacionRepository.GetByIdAsync(stop.UbicacionId, cancellationToken);

                    stopDtos.Add(new ViajeDetailDto.StopDto
                    {
                        Orden = stop.Orden,
                        Tipo = stop.Tipo,
                        Nombre = ubicacion?.Nombre ?? string.Empty,
                        Direccion = ubicacion?.Direccion ?? string.Empty,
                        Latitud = ubicacion?.Latitud ?? 0,
                        Longitud = ubicacion?.Longitud ?? 0
                    });
                }
            }

            return new ViajeDetailDto
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
                EventoNombre = evento?.Nombre,
                Ruta = new ViajeDetailDto.RutaDto
                {
                    TiempoEstimado = ruta?.TiempoEstimado ?? TimeSpan.Zero,
                    DistanciaKm = ruta?.DistanciaKm ?? 0,
                    Stops = stopDtos
                }
            };
        }
    }
}
