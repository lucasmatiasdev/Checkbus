using Checkbus.ApiService.Domain.Entities.Payments;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment, CancellationToken cancellationToken);

        Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<Payment?> GetByExternalOrderIdAsync(string externalOrderId, CancellationToken cancellationToken);

        Task UpdateAsync(Payment payment, CancellationToken cancellationToken);
    }
}
