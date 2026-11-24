using Checkbus.ApiService.Domain.Entities.Refunds;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IRefundRepository
    {

        Task AddAsync(Refund refund, CancellationToken cancellationToken);

        Task<Refund?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken);
    }
}
