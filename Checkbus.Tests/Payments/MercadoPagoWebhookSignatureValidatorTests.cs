using System.Security.Cryptography;
using System.Text;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Proves <see cref="MercadoPagoWebhookSignatureValidator"/> (odd/tasks/pagos-mercadopago.md
/// Fase D2) against a known secret/header set — no live Mercado Pago signature needed, the
/// expected HMAC is computed here with the exact same manifest template the validator itself
/// documents (<c>id:[data.id];request-id:[x-request-id];ts:[ts];</c>).
/// </summary>
public class MercadoPagoWebhookSignatureValidatorTests
{
    private const string Secret = "test-webhook-secret";
    private const string DataId = "123456789";
    private const string RequestId = "req-abc-123";
    private const string Ts = "1700000000";

    private static string ComputeValidHex(string dataId = DataId, string requestId = RequestId, string ts = Ts, string secret = Secret)
    {
        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void IsValid_CorrectlyComputedSignature_ReturnsTrue()
    {
        var validHex = ComputeValidHex();
        var header = $"ts={Ts},v1={validHex}";

        Assert.True(MercadoPagoWebhookSignatureValidator.IsValid(DataId, RequestId, header, Secret));
    }

    [Fact]
    public void IsValid_SignatureComputedWithWrongSecret_ReturnsFalse()
    {
        var tamperedHex = ComputeValidHex(secret: "a-different-secret");
        var header = $"ts={Ts},v1={tamperedHex}";

        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid(DataId, RequestId, header, Secret));
    }

    [Fact]
    public void IsValid_TamperedDataId_ReturnsFalse()
    {
        // Signature was computed for the original DataId; validating against a different one
        // (e.g. an attacker swapping which order a notification claims to be about) must fail.
        var validHex = ComputeValidHex();
        var header = $"ts={Ts},v1={validHex}";

        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid("999999999", RequestId, header, Secret));
    }

    [Fact]
    public void IsValid_TamperedRequestId_ReturnsFalse()
    {
        var validHex = ComputeValidHex();
        var header = $"ts={Ts},v1={validHex}";

        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid(DataId, "req-different", header, Secret));
    }

    [Fact]
    public void IsValid_TamperedV1Value_ReturnsFalse()
    {
        var validHex = ComputeValidHex();
        // Flip the last hex character to produce a value of the same length/shape that is
        // provably not the correct digest.
        var tampered = validHex[..^1] + (validHex[^1] == 'a' ? 'b' : 'a');
        var header = $"ts={Ts},v1={tampered}";

        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid(DataId, RequestId, header, Secret));
    }

    [Fact]
    public void IsValid_MissingXSignatureHeader_ReturnsFalse()
    {
        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid(DataId, RequestId, null, Secret));
        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid(DataId, RequestId, string.Empty, Secret));
    }

    [Fact]
    public void IsValid_MissingRequestIdHeader_ReturnsFalse()
    {
        var validHex = ComputeValidHex();
        var header = $"ts={Ts},v1={validHex}";

        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid(DataId, null, header, Secret));
    }

    [Fact]
    public void IsValid_MissingDataId_ReturnsFalse()
    {
        var validHex = ComputeValidHex();
        var header = $"ts={Ts},v1={validHex}";

        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid(null, RequestId, header, Secret));
    }

    [Theory]
    [InlineData("ts=1700000000")] // missing v1
    [InlineData("v1=abcd1234")] // missing ts
    [InlineData("garbage-no-equals-signs")]
    [InlineData("")]
    public void IsValid_MalformedSignatureHeader_ReturnsFalse(string malformedHeader)
    {
        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid(DataId, RequestId, malformedHeader, Secret));
    }

    [Fact]
    public void IsValid_BlankWebhookSecret_ReturnsFalse_EvenIfHeaderLooksWellFormed()
    {
        // Fail-closed posture: a not-yet-provisioned (blank) secret must never make every
        // signature "valid" against a meaningless empty HMAC key.
        var header = "ts=1700000000,v1=deadbeef";

        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid(DataId, RequestId, header, string.Empty));
        Assert.False(MercadoPagoWebhookSignatureValidator.IsValid(DataId, RequestId, header, null));
    }
}
