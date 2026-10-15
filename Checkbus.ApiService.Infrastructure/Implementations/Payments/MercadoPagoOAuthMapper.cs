using Checkbus.ApiService.Application.Interfaces.Payments;
using MercadoPago.Resource.OAuth;

namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// Pure request/response mapping for <see cref="MercadoPagoOAuthService"/>
    /// (odd/tasks/pagos-mercadopago.md Fase I, I4) — kept separate so it is unit-testable without
    /// a live HTTP call, the same "pure mapper" pattern Fase B established with
    /// <see cref="MercadoPagoOrderMapper"/>.
    /// </summary>
    public static class MercadoPagoOAuthMapper
    {
        /// <summary>
        /// Builds the authorize-URL Mercado Pago's own hosted page expects, per the confirmed
        /// template (odd/tasks/pagos-mercadopago.md Fase I): <c>client_id</c>,
        /// <c>response_type=code</c>, <c>platform_id=mp</c>, <c>redirect_uri</c>, and a
        /// <c>state</c> value combining <paramref name="organizationId"/> with the caller's own
        /// CSRF <paramref name="state"/> token (see <see cref="MercadoPagoOAuthService"/>'s own
        /// doc comment for why — the SDK's <c>GetAuthorizationURL</c> helper has no slot for a
        /// <c>state</c> parameter at all, confirmed by reflection).
        /// </summary>
        public static string BuildAuthorizationUrl(string clientId, string redirectUri, Guid organizationId, string state)
        {
            var combinedState = $"{organizationId}|{state}";

            return "https://auth.mercadopago.com/authorization" +
                $"?client_id={Uri.EscapeDataString(clientId)}" +
                "&response_type=code" +
                "&platform_id=mp" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&state={Uri.EscapeDataString(combinedState)}";
        }

        /// <summary>
        /// Maps the SDK's <see cref="OAuthCredential"/> resource onto this codebase's own
        /// <see cref="MercadoPagoOAuthTokenResult"/>. A missing <see cref="OAuthCredential.ExpiresIn"/>
        /// is treated conservatively as "already expired" (0 seconds) rather than assumed-valid.
        /// </summary>
        public static MercadoPagoOAuthTokenResult ToResult(OAuthCredential credential) => new(
            credential.AccessToken,
            credential.RefreshToken,
            credential.UserId.ToString(),
            DateTime.UtcNow.AddSeconds(credential.ExpiresIn ?? 0));
    }
}
