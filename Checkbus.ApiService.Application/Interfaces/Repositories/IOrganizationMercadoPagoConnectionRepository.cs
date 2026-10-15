using Checkbus.ApiService.Domain.Entities.Payments;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    /// <summary>
    /// CRUD port for <see cref="MercadoPagoConnection"/> (odd/tasks/pagos-mercadopago.md Fase I,
    /// I3). Every instance this interface hands to or receives from a caller holds PLAINTEXT
    /// <see cref="MercadoPagoConnection.AccessToken"/>/<see cref="MercadoPagoConnection.RefreshToken"/>
    /// values — the Infrastructure implementation is responsible for encrypting/decrypting at the
    /// storage boundary (ASP.NET Core Data Protection), so Application code never has to reason
    /// about ciphertext vs plaintext as a concern.
    /// </summary>
    public interface IOrganizationMercadoPagoConnectionRepository
    {
        /// <summary>Returns null if the Organization has never connected a Mercado Pago seller
        /// account — same "caller decides what a miss means" posture as
        /// <see cref="IPaymentRepository.GetByIdAsync"/>.</summary>
        Task<MercadoPagoConnection?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);

        /// <summary>
        /// Insert-or-replace: at most one connection per Organization. Reconnecting/re-authorizing
        /// (a brand-new OAuth grant) or a routine access-token refresh both call this — it always
        /// overwrites the existing row for that <see cref="MercadoPagoConnection.OrganizationId"/>
        /// rather than adding a second one.
        /// </summary>
        Task UpsertAsync(MercadoPagoConnection connection, CancellationToken cancellationToken);
    }
}
