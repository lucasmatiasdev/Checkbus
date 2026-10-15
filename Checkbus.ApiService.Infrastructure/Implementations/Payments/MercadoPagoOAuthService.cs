using Checkbus.ApiService.Application.Interfaces.Payments;
using MercadoPago.Client;
using MercadoPago.Client.OAuth;

namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// <see cref="IMercadoPagoOAuthService"/> adapter over the Mercado Pago OAuth endpoints,
    /// using the SDK's <see cref="OAuthClient"/> (odd/tasks/pagos-mercadopago.md Fase I, I4).
    ///
    /// <para><b>Real SDK shape, confirmed by reflecting the installed <c>mercadopago-sdk</c>
    /// 2.12.1 assembly (not assumed from the task's own wire-format description) — differs from
    /// the type name the task entry guessed ("OAuthCredentialRequest"):</b></para>
    /// <list type="bullet">
    /// <item><description><see cref="OAuthClient"/> exposes
    /// <c>CreateOAuthCredentialAsync(authorizationCode, clientId, clientSecret, redirectUri,
    /// requestOptions?, cancellationToken?)</c> directly — there is no public
    /// <c>OAuthCredentialRequest</c> type to construct; the request body is built internally by
    /// the SDK from these plain string parameters.</description></item>
    /// <item><description><see cref="OAuthClient.RefreshOAuthCredentialAsync"/> takes only
    /// <c>(refreshToken, requestOptions?, cancellationToken?)</c> — no explicit client id/secret
    /// parameters. The shipped XML docs for the internal
    /// <c>RefreshOAuthCredentialRequest.ClientSecret</c> property literally say "Client secret
    /// (Access Token)" — confirming the SDK repurposes <see cref="RequestOptions.AccessToken"/>
    /// to carry the platform app's client secret for this one call, exactly the way it normally
    /// carries a bearer token for every other SDK client.</description></item>
    /// <item><description>Neither the <c>CreateOAuthCredentialRequest</c> nor
    /// <c>RefreshOAuthCredentialRequest</c> internal shapes have a <c>State</c> property — the
    /// SDK has no mechanism to forward a CSRF <c>state</c> value into Mercado Pago's own
    /// token-exchange call. <see cref="ExchangeCodeAsync"/> still accepts it (for the caller's
    /// own CSRF bookkeeping) but does not forward it anywhere.</description></item>
    /// <item><description><see cref="OAuthClient.GetAuthorizationURL"/> exists but only takes
    /// <c>(appId, redirectUri, requestOptions?)</c> — no way to add a <c>state</c> query
    /// parameter. <see cref="BuildAuthorizationUrl"/> deliberately does NOT use it, and instead
    /// hand-builds the URL per the confirmed template
    /// (<c>https://auth.mercadopago.com/authorization?client_id=...&amp;response_type=code&amp;
    /// platform_id=mp&amp;redirect_uri=...&amp;state=...</c>), so a real CSRF token can be
    /// included.</description></item>
    /// </list>
    /// </summary>
    public class MercadoPagoOAuthService(MercadoPagoOptions options) : IMercadoPagoOAuthService
    {
        private readonly OAuthClient _oauthClient = new();

        public string BuildAuthorizationUrl(Guid organizationId, string state) =>
            MercadoPagoOAuthMapper.BuildAuthorizationUrl(options.ClientId, options.RedirectUri, organizationId, state);

        public async Task<MercadoPagoOAuthTokenResult> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken)
        {
            try
            {
                var credential = await _oauthClient.CreateOAuthCredentialAsync(
                    code,
                    options.ClientId,
                    options.ClientSecret,
                    options.RedirectUri,
                    cancellationToken: cancellationToken);

                return MercadoPagoOAuthMapper.ToResult(credential);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // No dedicated Domain exception exists for an OAuth failure (same posture
                // MercadoPagoPaymentService already uses for GetOrderAsync/RefundAsync — Fase A
                // only ever added OrderCreationFailedException for the order-create path
                // specifically).
                throw new InvalidOperationException("Mercado Pago OAuth code exchange failed.", ex);
            }
        }

        public async Task<MercadoPagoOAuthTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            try
            {
                // See this class's own doc comment: the SDK repurposes RequestOptions.AccessToken
                // to carry the platform app's ClientSecret for this specific call.
                var requestOptions = new RequestOptions { AccessToken = options.ClientSecret };
                var credential = await _oauthClient.RefreshOAuthCredentialAsync(refreshToken, requestOptions, cancellationToken);

                return MercadoPagoOAuthMapper.ToResult(credential);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException("Mercado Pago OAuth token refresh failed.", ex);
            }
        }
    }
}
