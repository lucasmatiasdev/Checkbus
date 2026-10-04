using Checkbus.ApiService.Application.Eventos.Queries;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Rutas;
using MediatR;

namespace Checkbus.ApiService.Application.Eventos.Commands
{
    public class CreateEventoCommandHandler : IRequestHandler<CreateEventoCommand, EventoDto>
    {
        private readonly IUbicacionRepository _ubicacionRepository;
        private readonly IEventoRepository _eventoRepository;

        public CreateEventoCommandHandler(IUbicacionRepository ubicacionRepository, IEventoRepository eventoRepository)
        {
            _ubicacionRepository = ubicacionRepository;
            _eventoRepository = eventoRepository;
        }

        public async Task<EventoDto> Handle(CreateEventoCommand request, CancellationToken cancellationToken)
        {
            // Dedupe by PlaceId first (shared Ubicacion catalog) — reuse the existing row when
            // the same real-world place was already picked for a previous Evento/Viaje, otherwise
            // create a new one. Evento+Ubicacion is not an atomic aggregate (unlike Viaje+Ruta+
            // Stops), so two separate repository calls are fine here per the task's own guidance.
            var ubicacion = await _ubicacionRepository.FindByPlaceIdAsync(request.PlaceId, cancellationToken);
            if (ubicacion is null)
            {
                var now = DateTime.UtcNow;
                ubicacion = new Ubicacion
                {
                    Id = Guid.NewGuid(),
                    Nombre = request.UbicacionNombre,
                    Direccion = request.Direccion,
                    PlaceId = request.PlaceId,
                    Latitud = request.Latitud,
                    Longitud = request.Longitud,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _ubicacionRepository.AddAsync(ubicacion, cancellationToken);
            }

            var createdAt = DateTime.UtcNow;
            var evento = new Evento
            {
                Id = Guid.NewGuid(),
                Nombre = request.Nombre,
                Tipo = request.Tipo,
                Fecha = request.Fecha,
                UbicacionId = ubicacion.Id,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            await _eventoRepository.AddAsync(evento, cancellationToken);

            return new EventoDto
            {
                Id = evento.Id,
                Nombre = evento.Nombre,
                Tipo = evento.Tipo,
                Fecha = evento.Fecha,
                Ubicacion = new EventoDto.UbicacionDto
                {
                    Nombre = ubicacion.Nombre,
                    Direccion = ubicacion.Direccion,
                    Latitud = ubicacion.Latitud,
                    Longitud = ubicacion.Longitud
                }
            };
        }
    }
}
