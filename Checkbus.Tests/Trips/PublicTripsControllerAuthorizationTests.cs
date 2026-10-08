using System.Net;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Opposite of <see cref="TripsControllerAuthorizationTests"/>: proves
/// <c>PublicTripsController.GetSeatMap</c> is truly <c>[AllowAnonymous]</c>, not
/// <c>[Authorize]</c> — a caller with NO bearer token at all must never get 401/403, same
/// convention (<see cref="CheckbusApiFactory"/>) as every other controller auth test in this
/// folder. Per that harness's documented dummy-Postgres-connection-string constraint, the
/// request still reaches the handler and fails downstream (DB unreachable) — this test only
/// proves the auth layer itself never rejects an anonymous caller, same shortcut rationale as
/// TripsControllerAuthorizationTests' Create_AllowedRole_IsNotRejectedAtAuthLayer.
/// </summary>
public class PublicTripsControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/public/trips";

    private readonly CheckbusApiFactory _factory;

    public PublicTripsControllerAuthorizationTests(CheckbusApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSeatMap_NoBearerToken_IsNotRejectedAtAuthLayer()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"{BasePath}/{Guid.NewGuid()}/seatmap", TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
