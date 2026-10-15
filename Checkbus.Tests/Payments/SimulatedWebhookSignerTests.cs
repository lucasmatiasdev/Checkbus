using System.Security.Cryptography;
using System.Text;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Proves <see cref="SimulatedWebhookSigner"/> (odd/tasks/pagos-mercadopago.md Fase J2) against
/// a known secret/order id/request id/timestamp — the expected HMAC is computed here with the
/// exact same manifest template <see cref="MercadoPagoWebhookSignatureValidator"/> itself
/// documents (<c>id:[data.id];request-id:[x-request-id];ts:[ts];</c>), same style as
/// <c>MercadoPagoWebhookSignatureValidatorTests</c> validating the real validator from the other
/// direction.
/// </summary>
public class SimulatedWebhookSignerTests
{
    private const string Secret = "sim-webhook-secret";
    private const string DataId = "SIM-order-123";
    private const string RequestId = "req-sim-abc";
    private static readonly DateTimeOffset Timestamp = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

    private static string ComputeExpectedHex(string dataId = DataId, string requestId = RequestId, long ts = 1_700_000_000, string secret = Secret)
    {
        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void ComputeSignature_KnownInputs_ProducesExactExpectedHexDigest()
    {
        var (ts, hexDigest) = SimulatedWebhookSigner.ComputeSignature(DataId, RequestId, Secret, Timestamp);

        Assert.Equal("1700000000", ts);
        Assert.Equal(ComputeExpectedHex(), hexDigest);
    }

    [Fact]
    public void BuildSignatureHeader_KnownInputs_MatchesTsEqualsCommaV1EqualsFormat()
    {
        var header = SimulatedWebhookSigner.BuildSignatureHeader(DataId, RequestId, Secret, Timestamp);

        Assert.Equal($"ts=1700000000,v1={ComputeExpectedHex()}", header);
    }

    [Fact]
    public void BuildSignatureHeader_DifferentDataId_ProducesDifferentDigest()
    {
        var header = SimulatedWebhookSigner.BuildSignatureHeader(DataId, RequestId, Secret, Timestamp);
        var otherHeader = SimulatedWebhookSigner.BuildSignatureHeader("SIM-order-999", RequestId, Secret, Timestamp);

        Assert.NotEqual(header, otherHeader);
    }

    [Fact]
    public void BuildSignatureHeader_OutputIsAccepted_ByTheRealSignatureValidator()
    {
        // The whole point of this helper (J2) is to produce a header the REAL
        // MercadoPagoWebhookSignatureValidator (Fase D2) accepts, not a parallel/incompatible
        // format — proves that end-to-end here, from the opposite direction of
        // MercadoPagoWebhookSignatureValidatorTests.
        var requestId = Guid.NewGuid().ToString();
        var header = SimulatedWebhookSigner.BuildSignatureHeader(DataId, requestId, Secret);

        Assert.True(MercadoPagoWebhookSignatureValidator.IsValid(DataId, requestId, header, Secret));
    }
}
