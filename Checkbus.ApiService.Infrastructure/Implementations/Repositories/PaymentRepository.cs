using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly CheckbusDbContext _context;

        public PaymentRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public Task<Payment?> GetByExternalOrderIdAsync(string externalOrderId, CancellationToken cancellationToken)
        {
            return _context.Payments.FirstOrDefaultAsync(p => p.ExternalOrderId == externalOrderId, cancellationToken);
        }

        public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
