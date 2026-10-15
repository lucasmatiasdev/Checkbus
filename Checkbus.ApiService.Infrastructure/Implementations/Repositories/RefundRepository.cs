using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Refunds;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class RefundRepository : IRefundRepository
    {
        private readonly CheckbusDbContext _context;

        public RefundRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Refund refund, CancellationToken cancellationToken)
        {
            _context.Refunds.Add(refund);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<Refund?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Refunds.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
        {
            return await _context.Refunds
                .Where(r => r.PaymentId == paymentId)
                .ToListAsync(cancellationToken);
        }
    }
}
