using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class SupportTicketRepository : ISupportTicketRepository
    {
        private readonly CheckbusDbContext _context;

        public SupportTicketRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken)
        {
            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.SupportTickets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<SupportTicket>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return await _context.SupportTickets
                .Where(x => x.OrganizationId == organizationId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.SupportTickets
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken)
        {
            _context.SupportTickets.Update(ticket);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
