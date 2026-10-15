using Checkbus.ApiService.Infrastructure.Implementations.Payments;
using MercadoPago.Resource.OAuth;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Unit tests for <see cref="MercadoPagoOAuthMapper"/> — the pure request/response mapping
/// between <see cref="MercadoPagoOAuthService"/> and the mercadopago-sdk 2.12.1 OAuth client
/// types (odd/tasks/pagos-mercadopago.md Fase I, I4). Same "pure mapper" testability pattern
/// Fase B established with <see cref="MercadoPagoOrderMapper"/>: the actual OAuth HTTP round-trip
/// (<c>OAuthClient.CreateOAuthCredentialAsync</c>/<c>RefreshOAuthCredentialAsync</c>) needs a real
/// Marketplace-model Mercado Pago app that does not exist yet (this Fase's own documented
/// blocker) — these tests exercise only the mapping logic, with a plain constructed SDK
/// <see cref="OAuthCredential"/> object (all its properties are public get/set, confirmed by
/// reflecting the installed assembly), no HTTP involved.
/// </summary>
public class MercadoPagoOAuthMapperTests
{
    [Fact]
    public void BuildAuthorizationUrl_IncludesClientIdResponseTypePlatformIdRedirectUriAndState()
    {
        var organizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var url = MercadoPagoOAuthMapper.BuildAuthorizationUrl(
            "APP-123", "https://checkbus.test/mp/callback", organizationId, "random-csrf-token");

        Assert.StartsWith("https://auth.mercadopago.com/authorization?", url);
        Assert.Contains("client_id=APP-123", url);
        Assert.Contains("response_type=code", url);
        Assert.Contains("platform_id=mp", url);
        Assert.Contains("redirect_uri=https%3A%2F%2Fcheckbus.test%2Fmp%2Fcallback", url);
        // organizationId and the caller's own CSRF token are combined into one opaque,
        // URL-encoded state value so the (not-yet-built) I7 callback can recover both.
        Assert.Contains(Uri.EscapeDataString($"{organizationId}|random-csrf-token"), url);
    }

    [Fact]
    public void BuildAuthorizationUrl_EscapesSpecialCharactersInRedirectUriAndState()
    {
        var organizationId = Guid.NewGuid();

        var url = MercadoPagoOAuthMapper.BuildAuthorizationUrl(
            "APP-123", "https://checkbus.test/mp/callback?x=1&y=2", organizationId, "token with spaces & stuff");

        // A raw '&' from the redirect_uri/state must never leak in as a literal query separator.
        Assert.DoesNotContain("x=1&y=2", url);
        Assert.Contains(Uri.EscapeDataString("https://checkbus.test/mp/callback?x=1&y=2"), url);
    }

    [Fact]
    public void ToResult_MapsAccessTokenRefreshTokenAndUserId()
    {
        var credential = new OAuthCredential
        {
            AccessToken = "APP_USR-access-token",
            RefreshToken = "TG-refresh-token",
            UserId = 987654321,
            ExpiresIn = 15552000 // 180 days, Mercado Pago's documented value
        };

        var before = DateTime.UtcNow;
        var result = MercadoPagoOAuthMapper.ToResult(credential);
        var after = DateTime.UtcNow;

        Assert.Equal("APP_USR-access-token", result.AccessToken);
        Assert.Equal("TG-refresh-token", result.RefreshToken);
        Assert.Equal("987654321", result.SellerUserId);
        // ExpiresAt = now + expires_in seconds, computed at mapping time — bounded by the two
        // "now" snapshots taken immediately around the call, plus the full 180-day offset.
        Assert.InRange(result.ExpiresAt, before.AddSeconds(15552000), after.AddSeconds(15552000));
    }

    [Fact]
    public void ToResult_MissingExpiresIn_TreatsItAsAlreadyExpiredRatherThanAssumedValid()
    {
        var credential = new OAuthCredential
        {
            AccessToken = "APP_USR-access-token",
            RefreshToken = "TG-refresh-token",
            UserId = 1,
            ExpiresIn = null
        };

        var before = DateTime.UtcNow;
        var result = MercadoPagoOAuthMapper.ToResult(credential);
        var after = DateTime.UtcNow;

        Assert.InRange(result.ExpiresAt, before, after);
    }
}
