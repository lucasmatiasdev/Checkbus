using Checkbus.ApiService.Domain.Entities.Invoices;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IInvoiceRepository
    {
        // Fase D creates one Invoice per passenger once the covering Payment reaches
        // Aprobado — see Invoice's own doc comment.
        Task AddAsync(Invoice invoice, CancellationToken cancellationToken);

        Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
