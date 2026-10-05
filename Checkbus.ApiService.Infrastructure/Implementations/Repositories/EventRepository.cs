using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly CheckbusDbContext _context;

        public EventRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Event @event, CancellationToken cancellationToken)
        {
            _context.Events.Add(@event);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Events.ToListAsync(cancellationToken);
        }
    }
}
