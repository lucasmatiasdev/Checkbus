using Checkbus.ApiService.Domain.Entities.Invoices;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IInvoiceRepository
    {

        Task AddAsync(Invoice invoice, CancellationToken cancellationToken);

        Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
