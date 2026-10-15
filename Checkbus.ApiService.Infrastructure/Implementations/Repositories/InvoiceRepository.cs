using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Invoices;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly CheckbusDbContext _context;

        public InvoiceRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Invoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }
    }
}
