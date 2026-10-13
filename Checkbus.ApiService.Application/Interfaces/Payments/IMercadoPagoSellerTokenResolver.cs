namespace Checkbus.ApiService.Application.Interfaces.Payments
{
    /// <summary>
    /// Resolves the Mercado Pago seller access token to use for a given Organization's real
    /// orders/refunds (odd/tasks/pagos-mercadopago.md Fase I, I6) — refreshing it first if it is
    /// near/past expiry, persisting the rotated token pair. There is no global fallback token:
    /// an Organization with no connected Mercado Pago seller account cannot be charged/refunded
    /// at all, by design (Fase I's whole point is per-Organization money, not Checkbus's own).
    /// </summary>
    public interface IMercadoPagoSellerTokenResolver
    {
        /// <summary>
        /// Throws <see cref="Checkbus.ApiService.Domain.Exceptions.Payments.OrganizationNotConnectedToMercadoPagoException"/>
        /// if <paramref name="organizationId"/> has no <c>MercadoPagoConnection</c> row at all.
        /// </summary>
        Task<string> ResolveAccessTokenAsync(Guid organizationId, CancellationToken cancellationToken);
    }
}
