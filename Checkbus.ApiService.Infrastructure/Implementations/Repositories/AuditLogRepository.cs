using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Audit;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly CheckbusDbContext _context;

        public AuditLogRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            _context.AuditLogEntries.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AuditLogEntry>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return await _context.AuditLogEntries
                .Where(x => x.OrganizationId == organizationId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AuditLogEntry>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.AuditLogEntries
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
