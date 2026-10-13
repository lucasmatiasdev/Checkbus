namespace Checkbus.ApiService.Domain.Entities.Payments
{
    // One row per Organization (Fase I, Marketplace/Split Payments model —
    // odd/tasks/pagos-mercadopago.md): links a tenant bus company's own Mercado Pago seller
    // account to Checkbus via OAuth, so a real order is created/refunded against THAT
    // organization's own money, not Checkbus's single global account. FK-only, no navigation
    // property to Organization, same posture as every other entity in this codebase.
    // OrganizationId is unique (enforced by CheckbusDbContext, not here) — at most one
    // connection per Organization; reconnecting overwrites the existing row rather than adding
    // a second one (IOrganizationMercadoPagoConnectionRepository.UpsertAsync).
    //
    // AccessToken/RefreshToken are stored ENCRYPTED AT REST (ASP.NET Core Data Protection,
    // IDataProtector, applied by OrganizationMercadoPagoConnectionRepository — Infrastructure,
    // not here). This is a genuinely new pattern for this codebase: every other string column
    // on every other entity is stored as plain text. The reason these two specifically are not:
    // they are OAuth bearer credentials that let Checkbus create and refund real-money Mercado
    // Pago orders on behalf of the organization's own seller account for up to 180 days
    // (RefreshToken is rotated on every use and effectively extends that indefinitely) — a
    // leaked value here is a direct financial-fraud vector (whoever has it can move the
    // organization's own money), not merely a privacy concern the way a name or email is. The
    // Domain entity itself only ever holds the opaque ciphertext string in these two
    // properties — Application code that reads/writes a MercadoPagoConnection never has to
    // reason about "is this plaintext or ciphertext" as a concern; only the repository
    // (Infrastructure) knows these bytes are protected and calls Protect/Unprotect.
    public class MercadoPagoConnection
    {
        public Guid Id { get; set; }
        public required Guid OrganizationId { get; set; }

        // Mercado Pago's own collector/seller id for the connected account (the OAuth token
        // response's "user_id" field) — not a Checkbus concept, kept verbatim for support/
        // reconciliation purposes.
        public required string SellerUserId { get; set; }

        // Encrypted at rest — see class doc comment. Opaque ciphertext from this entity's point
        // of view.
        public required string AccessToken { get; set; }

        // Encrypted at rest — see class doc comment. Mercado Pago rotates this value on every
        // refresh call; the old value stops working the moment a new one is issued, so this
        // column is always overwritten in full, never appended to.
        public required string RefreshToken { get; set; }

        // UTC. Computed at exchange/refresh time as "now + expires_in seconds" (Mercado Pago's
        // access tokens are valid 180 days) — used by the token-resolution service to decide
        // whether to refresh before using this connection.
        public required DateTime ExpiresAt { get; set; }

        // UTC. When this Organization first completed the OAuth flow. Not updated on refresh
        // (refresh changes AccessToken/RefreshToken/ExpiresAt only) — a fresh reconnect
        // (UpsertAsync replacing the row) does update it, since that is a brand-new grant.
        public required DateTime ConnectedAt { get; set; }
    }
}
