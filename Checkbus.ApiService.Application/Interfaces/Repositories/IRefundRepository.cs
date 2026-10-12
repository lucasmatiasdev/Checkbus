using Checkbus.ApiService.Domain.Entities.Refunds;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IRefundRepository
    {
        // Fase F persists one Refund per cancelled Reservation, referencing both the covering
        // Payment and that specific Reservation — see Refund's own doc comment.
        Task AddAsync(Refund refund, CancellationToken cancellationToken);

        Task<Refund?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        // Added for Fase F1's partial-vs-full refund logic: to decide whether cancelling one
        // seat is the LAST still-active seat on its Payment (full refund) or one of several
        // (partial refund), the handler needs every Refund already issued against that Payment
        // so far. Just the read method for now — the actual partial/full decision logic is
        // Fase F's job, not Fase B's.
        Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken);
    }
}
