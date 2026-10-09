using System.Net;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <c>PublicReservationsController.GetConfirmation</c> is truly
/// <c>[AllowAnonymous]</c>, not <c>[Authorize]</c> — same convention as
/// <see cref="PublicTripsControllerAuthorizationTests"/>. Per that harness's documented
/// dummy-Postgres-connection-string constraint, the request still reaches the handler and
/// fails downstream (DB unreachable) — this test only proves the auth layer itself never
/// rejects an anonymous caller.
/// </summary>
public class PublicReservationsControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/public/reservations";

    private readonly CheckbusApiFactory _factory;

    public PublicReservationsControllerAuthorizationTests(CheckbusApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetConfirmation_NoBearerToken_IsNotRejectedAtAuthLayer()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"{BasePath}?ids={Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
