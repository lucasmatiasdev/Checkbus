using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    /// <summary>
    /// <see cref="IOrganizationMercadoPagoConnectionRepository"/> EF implementation
    /// (odd/tasks/pagos-mercadopago.md Fase I, I4) — the ONE place in this codebase that knows
    /// <see cref="MercadoPagoConnection.AccessToken"/>/<see cref="MercadoPagoConnection.RefreshToken"/>
    /// are encrypted at rest. Uses ASP.NET Core's Data Protection API
    /// (<see cref="IDataProtectionProvider"/>/<see cref="IDataProtector"/>) rather than a
    /// hand-rolled cipher: it is already part of the ASP.NET Core stack (no new crypto surface to
    /// get wrong), handles key management/rotation transparently, and every value it protects is
    /// tied to this specific purpose string ("MercadoPagoConnection.Tokens") so a protector
    /// created for a different purpose elsewhere in the app could never accidentally decrypt
    /// these columns.
    ///
    /// Every <see cref="MercadoPagoConnection"/> this class hands back to a caller (via
    /// <see cref="GetByOrganizationIdAsync"/>) has PLAINTEXT tokens — decryption happens here, not
    /// in Application. Every connection a caller passes INTO <see cref="UpsertAsync"/> is assumed
    /// to carry plaintext tokens (exactly what <see cref="MercadoPagoOAuthService"/>'s
    /// <c>ExchangeCodeAsync</c>/<c>RefreshAsync</c> return) — this class encrypts them before
    /// anything touches the database.
    /// </summary>
    public class OrganizationMercadoPagoConnectionRepository : IOrganizationMercadoPagoConnectionRepository
    {
        private readonly CheckbusDbContext _context;
        private readonly IDataProtector _protector;

        public OrganizationMercadoPagoConnectionRepository(CheckbusDbContext context, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _protector = dataProtectionProvider.CreateProtector("MercadoPagoConnection.Tokens");
        }

        public async Task<MercadoPagoConnection?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            var entity = await _context.MercadoPagoConnections
                .FirstOrDefaultAsync(c => c.OrganizationId == organizationId, cancellationToken);

            return entity is null ? null : Decrypted(entity);
        }

        public async Task UpsertAsync(MercadoPagoConnection connection, CancellationToken cancellationToken)
        {
            var existing = await _context.MercadoPagoConnections
                .FirstOrDefaultAsync(c => c.OrganizationId == connection.OrganizationId, cancellationToken);

            if (existing is null)
            {
                _context.MercadoPagoConnections.Add(new MercadoPagoConnection
                {
                    Id = connection.Id == Guid.Empty ? Guid.NewGuid() : connection.Id,
                    OrganizationId = connection.OrganizationId,
                    SellerUserId = connection.SellerUserId,
                    AccessToken = _protector.Protect(connection.AccessToken),
                    RefreshToken = _protector.Protect(connection.RefreshToken),
                    ExpiresAt = connection.ExpiresAt,
                    ConnectedAt = connection.ConnectedAt
                });
            }
            else
            {
                // Overwrite in place — "at most one connection per Organization" (I1/I3's own
                // doc comments). A fresh reconnect and a routine refresh both land here; both
                // replace the old (now-invalid, per Mercado Pago's rotation rule) RefreshToken in
                // full.
                existing.SellerUserId = connection.SellerUserId;
                existing.AccessToken = _protector.Protect(connection.AccessToken);
                existing.RefreshToken = _protector.Protect(connection.RefreshToken);
                existing.ExpiresAt = connection.ExpiresAt;
                existing.ConnectedAt = connection.ConnectedAt;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private MercadoPagoConnection Decrypted(MercadoPagoConnection source) => new()
        {
            Id = source.Id,
            OrganizationId = source.OrganizationId,
            SellerUserId = source.SellerUserId,
            AccessToken = _protector.Unprotect(source.AccessToken),
            RefreshToken = _protector.Unprotect(source.RefreshToken),
            ExpiresAt = source.ExpiresAt,
            ConnectedAt = source.ConnectedAt
        };
    }
}
