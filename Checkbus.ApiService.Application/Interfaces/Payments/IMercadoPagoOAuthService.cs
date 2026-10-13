namespace Checkbus.ApiService.Application.Interfaces.Payments
{
    /// <summary>
    /// Mercado Pago Marketplace OAuth port (odd/tasks/pagos-mercadopago.md Fase I, I3) — lets an
    /// Organization connect its own Mercado Pago seller account to Checkbus so real orders use
    /// THAT organization's token instead of one global Checkbus-owned token. Infrastructure talks
    /// to Mercado Pago's OAuth endpoints via the SDK's <c>OAuthClient</c>; Application decides
    /// when to call this (I6/I7).
    /// </summary>
    public interface IMercadoPagoOAuthService
    {
        /// <summary>
        /// Builds the URL to redirect an Organization's admin to so they can approve the
        /// connection on Mercado Pago's own hosted page. <paramref name="state"/> is a
        /// caller-generated random CSRF token; the implementation correlates it with
        /// <paramref name="organizationId"/> so the callback (I7, not built in this Fase) can
        /// recover which Organization initiated the flow once Mercado Pago redirects back with
        /// the same (opaque, combined) state value.
        /// </summary>
        string BuildAuthorizationUrl(Guid organizationId, string state);

        /// <summary>
        /// Exchanges a one-time, 10-minute-TTL authorization <paramref name="code"/> (from Mercado
        /// Pago's redirect) for a real access/refresh token pair. <paramref name="state"/> is
        /// accepted for the caller's own CSRF bookkeeping (comparing it against what
        /// <see cref="BuildAuthorizationUrl"/> handed out) — the installed SDK's OAuth request
        /// shape has no slot to forward it to Mercado Pago itself, confirmed by reflecting the
        /// installed <c>mercadopago-sdk</c> 2.12.1 assembly's
        /// <c>MercadoPago.Client.OAuth.CreateOAuthCredentialRequest</c> (no <c>State</c> property).
        /// </summary>
        Task<MercadoPagoOAuthTokenResult> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken);

        /// <summary>
        /// Refreshes an access token. Mercado Pago rotates the refresh token on every call — the
        /// returned <see cref="MercadoPagoOAuthTokenResult.RefreshToken"/> is a NEW value that
        /// must replace the old one in storage; the old one stops working the moment this call
        /// succeeds.
        /// </summary>
        Task<MercadoPagoOAuthTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Result of a successful OAuth code exchange or refresh. <see cref="ExpiresAt"/> is computed
    /// as <c>DateTime.UtcNow + expires_in seconds</c> at the moment the token was issued (Mercado
    /// Pago's access tokens are valid 180 days) — callers persist this absolute instant, not the
    /// raw relative <c>expires_in</c> Mercado Pago returns.
    /// </summary>
    public record MercadoPagoOAuthTokenResult(
        string AccessToken,
        string RefreshToken,
        string SellerUserId,
        DateTime ExpiresAt);
}
