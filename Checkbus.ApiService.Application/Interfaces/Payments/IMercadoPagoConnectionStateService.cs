namespace Checkbus.ApiService.Application.Interfaces.Payments
{
    /// <summary>
    /// Builds and validates the opaque, tamper-proof <c>state</c> value I7
    /// (odd/tasks/pagos-mercadopago.md Fase I) hands to
    /// <see cref="IMercadoPagoOAuthService.BuildAuthorizationUrl"/> and recovers from the OAuth
    /// callback. Deliberately a separate Application port from <see cref="IMercadoPagoOAuthService"/>
    /// — it has nothing to do with talking to Mercado Pago itself, only with this server's own
    /// CSRF/account-linking guarantee, implemented in Infrastructure with ASP.NET Core Data
    /// Protection (same split as <see cref="Checkbus.ApiService.Application.Interfaces.Repositories.IOrganizationMercadoPagoConnectionRepository"/>
    /// over <c>OrganizationMercadoPagoConnectionRepository</c>'s own <c>IDataProtector</c> usage,
    /// and for the same reason: Application code should never need to reason about ciphertext).
    ///
    /// <para><b>Why this exists at all, not just a raw random CSRF token:</b>
    /// <see cref="IMercadoPagoOAuthService.BuildAuthorizationUrl"/> already combines
    /// <c>organizationId</c> with whatever <c>state</c> string it is given into one
    /// <c>"{organizationId}|{state}"</c> value (I3/I4's own confirmed, unchangeable shape) and
    /// sends THAT to Mercado Pago as its own <c>state</c> query parameter — Mercado Pago echoes
    /// it back verbatim on the callback redirect. The <c>organizationId</c> prefix in that echoed
    /// value is plain cleartext and must never be trusted on its own: nothing stops a malicious
    /// caller from hitting the callback endpoint directly with a forged
    /// <c>state=&lt;victim-org-id&gt;|&lt;anything&gt;</c> and a real <c>code</c> they obtained by
    /// authorizing their OWN Mercado Pago seller account, tricking the callback into linking
    /// THEIR seller connection to the VICTIM organization. Protecting an independent payload
    /// (<see cref="Protect"/>) that carries its own <c>organizationId</c> inside the ciphertext —
    /// and using THAT recovered value as the only trusted source of truth on
    /// <see cref="TryUnprotect"/> — defeats this: forging a ciphertext that decrypts to an
    /// attacker-chosen organizationId requires this server's own Data Protection keys, which an
    /// outside caller never has.</para>
    /// </summary>
    public interface IMercadoPagoConnectionStateService
    {
        /// <summary>
        /// Produces an opaque, encrypted string carrying <paramref name="organizationId"/> and
        /// the current UTC instant. This is the value to pass as <c>state</c> into
        /// <see cref="IMercadoPagoOAuthService.BuildAuthorizationUrl"/> — NOT the final combined
        /// value Mercado Pago actually receives, which that method builds on top of this one.
        /// </summary>
        string Protect(Guid organizationId);

        /// <summary>
        /// Recovers the <c>organizationId</c> from a value previously returned by
        /// <see cref="Protect"/>. Returns <see cref="MercadoPagoConnectionStateResult.IsValid"/>
        /// <c>false</c> (never throws) for any undecryptable, malformed, or expired value —
        /// expiry uses the same 10-minute window Mercado Pago itself documents for its one-time
        /// authorization code, so a stale callback request can never resurrect an old state.
        /// </summary>
        MercadoPagoConnectionStateResult TryUnprotect(string protectedState);
    }

    public record MercadoPagoConnectionStateResult(bool IsValid, Guid OrganizationId);
}
