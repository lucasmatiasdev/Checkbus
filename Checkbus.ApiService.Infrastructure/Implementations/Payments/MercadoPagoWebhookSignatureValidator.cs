using System.Security.Cryptography;
using System.Text;

namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// Validates a Mercado Pago webhook notification's <c>x-signature</c> HMAC
    /// (odd/tasks/pagos-mercadopago.md Fase D2). Static and side-effect free so D6's tests can
    /// exercise it directly without a live HTTP pipeline.
    ///
    /// <para><b>Facts below are confirmed via Mercado Pago's own official docs, not guessed —
    /// see the task's own "Signature validation facts confirmed via official docs" note:</b>
    /// headers are <c>x-signature</c> (format <c>ts=&lt;unix-ts&gt;,v1=&lt;hex-hmac&gt;</c>) and
    /// <c>x-request-id</c>. The manifest string to HMAC-SHA256 (hex digest, key =
    /// <c>WebhookSecret</c>) is the literal template
    /// <c>id:[data.id];request-id:[x-request-id];ts:[ts];</c> — colon-after-label,
    /// semicolon-terminated, in exactly this order. <c>data.id</c> comes from the webhook URL's
    /// own query string, never the JSON body. The computed digest is compared against the
    /// <c>v1=</c> value with <see cref="CryptographicOperations.FixedTimeEquals"/> (constant-time,
    /// avoids a timing side-channel on the comparison).</para>
    ///
    /// <para><b>Security posture:</b> callers must never log the secret, the provided
    /// signature, or the computed HMAC — only that validation passed/failed (see the task's own
    /// explicit instruction on this). This class itself logs nothing.</para>
    /// </summary>
    public static class MercadoPagoWebhookSignatureValidator
    {
        /// <summary>
        /// True only if every required input is present, the <c>x-signature</c> header parses
        /// into both a <c>ts</c> and a <c>v1</c> part, and the computed HMAC matches <c>v1</c>
        /// exactly (constant-time comparison). A blank <paramref name="webhookSecret"/> (e.g.
        /// not yet provisioned for this environment — see MercadoPagoOptions's own permissive
        /// binding) fails CLOSED here rather than validating against a meaningless empty key.
        /// </summary>
        public static bool IsValid(
            string? dataId, string? requestId, string? signatureHeader, string? webhookSecret)
        {
            if (string.IsNullOrEmpty(dataId)
                || string.IsNullOrEmpty(requestId)
                || string.IsNullOrEmpty(signatureHeader)
                || string.IsNullOrEmpty(webhookSecret))
            {
                return false;
            }

            if (!TryParseSignatureHeader(signatureHeader, out var ts, out var v1))
            {
                return false;
            }

            var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
            var computedHex = ComputeHmacHex(manifest, webhookSecret);

            return FixedTimeEqualsHex(computedHex, v1);
        }

        // x-signature's value is a comma-separated list of key=value pairs, e.g.
        // "ts=1704908010,v1=7f8a9...". Order is not guaranteed by the format itself, so this
        // parses by key rather than assuming position.
        private static bool TryParseSignatureHeader(string header, out string ts, out string v1)
        {
            ts = string.Empty;
            v1 = string.Empty;

            foreach (var part in header.Split(','))
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2)
                {
                    continue;
                }

                var key = kv[0].Trim();
                var value = kv[1].Trim();

                if (key.Equals("ts", StringComparison.OrdinalIgnoreCase))
                {
                    ts = value;
                }
                else if (key.Equals("v1", StringComparison.OrdinalIgnoreCase))
                {
                    v1 = value;
                }
            }

            return !string.IsNullOrEmpty(ts) && !string.IsNullOrEmpty(v1);
        }

        private static string ComputeHmacHex(string manifest, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool FixedTimeEqualsHex(string computedHex, string providedHex)
        {
            // Different lengths can only mean "not equal" (e.g. a malformed/truncated v1) —
            // safe to short-circuit before the constant-time compare, which requires equal-
            // length inputs anyway.
            if (computedHex.Length != providedHex.Length)
            {
                return false;
            }

            byte[] computedBytes;
            byte[] providedBytes;
            try
            {
                computedBytes = Convert.FromHexString(computedHex);
                providedBytes = Convert.FromHexString(providedHex);
            }
            catch (FormatException)
            {
                // Not valid hex (e.g. a tampered/garbage v1 value) — definitely not equal.
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(computedBytes, providedBytes);
        }
    }
}
