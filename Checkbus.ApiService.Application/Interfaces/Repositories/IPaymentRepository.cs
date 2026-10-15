using Checkbus.ApiService.Domain.Entities.Payments;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment, CancellationToken cancellationToken);

        Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        // Added for Fase D: the Mercado Pago webhook notification carries the order id
        // (Payment.ExternalOrderId), not our own Payment.Id — this is how
        // HandlePaymentWebhookCommandHandler resolves a notification back to the right Payment
        // row. Returns null for an order id that doesn't resolve rather than throwing, same
        // "caller decides what a miss means" posture as IReservationRepository.GetByIdAsync.
        Task<Payment?> GetByExternalOrderIdAsync(string externalOrderId, CancellationToken cancellationToken);

        // Needed once ExternalOrderId/Status are set after the Mercado Pago call succeeds, and
        // again whenever Status transitions later (webhook approval/rejection, Fase F refund).
        Task UpdateAsync(Payment payment, CancellationToken cancellationToken);
    }
}
