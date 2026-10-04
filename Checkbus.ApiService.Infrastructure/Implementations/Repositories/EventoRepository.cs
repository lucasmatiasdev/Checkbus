using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class EventoRepository : IEventoRepository
    {
        private readonly CheckbusDbContext _context;

        public EventoRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Evento evento, CancellationToken cancellationToken)
        {
            _context.Eventos.Add(evento);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<Evento?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Eventos.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Evento>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Eventos.ToListAsync(cancellationToken);
        }
    }
}
