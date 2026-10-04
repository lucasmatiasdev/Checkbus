using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Rutas;
using MediatR;

namespace Checkbus.ApiService.Application.Eventos.Queries
{
    public class GetEventosQueryHandler : IRequestHandler<GetEventosQuery, IReadOnlyList<EventoDto>>
    {
        private readonly IEventoRepository _eventoRepository;
        private readonly IUbicacionRepository _ubicacionRepository;

        public GetEventosQueryHandler(IEventoRepository eventoRepository, IUbicacionRepository ubicacionRepository)
        {
            _eventoRepository = eventoRepository;
            _ubicacionRepository = ubicacionRepository;
        }

        public async Task<IReadOnlyList<EventoDto>> Handle(GetEventosQuery request, CancellationToken cancellationToken)
        {
            var eventos = await _eventoRepository.GetAllAsync(cancellationToken);

            // IUbicacionRepository has no batch-by-ids lookup, so resolve each distinct
            // Ubicacion at most once rather than once per Evento — avoids N+1 when multiple
            // eventos share the same venue, mirroring GetMaintenanceRecordsQueryHandler's
            // single-join-table approach without inventing a new repository method for this.
            var ubicacionesById = new Dictionary<Guid, Ubicacion>();
            foreach (var ubicacionId in eventos.Select(e => e.UbicacionId).Distinct())
            {
                var ubicacion = await _ubicacionRepository.GetByIdAsync(ubicacionId, cancellationToken);
                if (ubicacion is not null)
                {
                    ubicacionesById[ubicacionId] = ubicacion;
                }
            }

            return eventos.Select(e => MapToDto(e, ubicacionesById)).ToList();
        }

        private static EventoDto MapToDto(Evento evento, Dictionary<Guid, Ubicacion> ubicacionesById)
        {
            ubicacionesById.TryGetValue(evento.UbicacionId, out var ubicacion);

            return new EventoDto
            {
                Id = evento.Id,
                Nombre = evento.Nombre,
                Tipo = evento.Tipo,
                Fecha = evento.Fecha,
                Ubicacion = new EventoDto.UbicacionDto
                {
                    Nombre = ubicacion?.Nombre ?? string.Empty,
                    Direccion = ubicacion?.Direccion ?? string.Empty,
                    Latitud = ubicacion?.Latitud ?? 0,
                    Longitud = ubicacion?.Longitud ?? 0
                }
            };
        }
    }
}
