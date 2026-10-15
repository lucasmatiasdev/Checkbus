using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// Builds a correctly-signed <c>x-signature</c> header value for the fake webhook
    /// notification <see cref="SimulatedPaymentService"/>'s checkout controller (J2) POSTs to
    /// this same app's own <c>PaymentsWebhookController</c> — odd/tasks/pagos-mercadopago.md
    /// Fase J2. Pure/static and side-effect free, same testability posture as
    /// <see cref="MercadoPagoOrderMapper"/>/<see cref="MercadoPagoWebhookSignatureValidator"/>.
    ///
    /// <para><b>Exact mirror of <see cref="MercadoPagoWebhookSignatureValidator"/>'s own
    /// confirmed algorithm (do not re-derive) — the header format is
    /// <c>ts=&lt;unix-ts&gt;,v1=&lt;hex-hmac&gt;</c>; the HMAC-SHA256 hex digest (key =
    /// <c>MercadoPagoOptions.WebhookSecret</c>) is over the literal manifest
    /// <c>id:[data.id];request-id:[x-request-id];ts:[ts];</c> (colon-after-label,
    /// semicolon-terminated, in exactly <c>id</c>, <c>request-id</c>, <c>ts</c> order).</b></para>
    /// </summary>
    public static class SimulatedWebhookSigner
    {
        /// <summary>
        /// Computes the Unix timestamp and hex HMAC digest for a given <paramref name="dataId"/>
        /// (the order id)/<paramref name="requestId"/> pair, against <paramref name="webhookSecret"/>.
        /// Exposed separately from <see cref="BuildSignatureHeader"/> so a test can assert the raw
        /// hex digest against an independently-computed expectation, the same way
        /// <c>MercadoPagoWebhookSignatureValidatorTests</c> validates the real validator from the
        /// other direction. <paramref name="timestamp"/> is optional/injectable for deterministic
        /// tests — defaults to <see cref="DateTimeOffset.UtcNow"/>.
        /// </summary>
        public static (string Ts, string HexDigest) ComputeSignature(
            string dataId, string requestId, string webhookSecret, DateTimeOffset? timestamp = null)
        {
            var ts = (timestamp ?? DateTimeOffset.UtcNow)
                .ToUnixTimeSeconds()
                .ToString(CultureInfo.InvariantCulture);

            var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
            var hex = Convert.ToHexString(hash).ToLowerInvariant();

            return (ts, hex);
        }

        /// <summary>
        /// Builds the full <c>x-signature</c> header value (<c>ts=...,v1=...</c>) ready to send
        /// on the fake webhook request — the exact shape
        /// <see cref="MercadoPagoWebhookSignatureValidator.IsValid"/> parses.
        /// </summary>
        public static string BuildSignatureHeader(
            string dataId, string requestId, string webhookSecret, DateTimeOffset? timestamp = null)
        {
            var (ts, hexDigest) = ComputeSignature(dataId, requestId, webhookSecret, timestamp);
            return $"ts={ts},v1={hexDigest}";
        }
    }
}
