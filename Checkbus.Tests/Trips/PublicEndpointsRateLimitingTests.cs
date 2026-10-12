using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves the "public-reservations" rate-limit policy (<c>Program.cs</c>,
/// odd/tasks/reservas-publicas.md R6) is actually wired to the public, unauthenticated
/// surface and nowhere else.
/// </summary>
/// <remarks>
/// Uses <see cref="CheckbusApiFactory"/> (in-process <c>TestServer</c>), the same lightweight
/// harness as every other controller authorization test in this folder, rather than the slow
/// Aspire/Postgres E2E harness (<see cref="Checkbus.Tests.Infrastructure.AspireE2ECollection"/>)
/// — <c>UseRateLimiter()</c> runs in the real middleware pipeline before any controller/handler
/// code executes, so the limiter is fully exercised regardless of what the handler would do.
///
/// Both tests deliberately target an endpoint+payload combination that FluentValidation rejects
/// with 400 BEFORE the handler ever touches the DB (same "structural rule short-circuits before
/// any repository call" shortcut used throughout this test folder, e.g.
/// TripsControllerAuthorizationTests.Create_AllowedRole_IsNotRejectedAtAuthLayer). This matters
/// here specifically: this harness's dummy/unreachable Postgres connection string means any
/// request that actually reached a handler's DB call could hang on a real (slow) Npgsql
/// connection-timeout per call, which is unacceptable inside a tight burst loop — an earlier
/// version of this test used GetSeatMap/GetAll, both DB-touching, and the burst loop stalled for
/// minutes as a result.
///
/// Exercises the REAL policy configured in Program.cs (20 requests/minute/IP, sliding window)
/// rather than a separate test-only policy with different numbers: an in-process TestServer
/// burst of ~25 requests completes in well under a second, so there is no flake risk from the
/// 1-minute window boundary, and testing the actual configured numbers is more meaningful than
/// a synthetic stand-in.
/// </remarks>
public class PublicEndpointsRateLimitingTests : IClassFixture<CheckbusApiFactory>
{
    private const string PublicCreateReservationPathPrefix = "/api/public/trips";
    private const string AuthenticatedTripsPath = "/api/trips";

    // One over the configured PermitLimit (20) in Program.cs's "public-reservations" policy —
    // enough to deterministically trip the limiter without depending on exact timing.
    private const int BurstCount = 25;

    private readonly CheckbusApiFactory _factory;

    public PublicEndpointsRateLimitingTests(CheckbusApiFactory factory)
    {
        _factory = factory;
    }

    private static string CreateToken(Role role) =>
        new JwtGenerator(CheckbusApiFactory.CreateTestJwtOptions()).GenerateToken(TestUserFactory.CreateUser(role));

    [Fact]
    public async Task PublicCreateReservation_BurstBeyondConfiguredLimit_EventuallyReturns429()
    {
        // Empty Passengers list -> CreatePublicReservationCommandValidator's NotEmpty rule
        // rejects it with 400 before CreatePublicReservationCommandHandler runs, so every
        // request that gets past the rate limiter fails fast with no DB access.
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var emptyBatchBody = new { Passengers = Array.Empty<object>() };

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < BurstCount; i++)
        {
            var response = await client.PostAsJsonAsync(
                $"{PublicCreateReservationPathPrefix}/{Guid.NewGuid()}/reservations",
                emptyBatchBody,
                cancellationToken);
            statuses.Add(response.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
        Assert.All(
            statuses.Where(s => s != HttpStatusCode.TooManyRequests),
            status => Assert.Equal(HttpStatusCode.BadRequest, status));
    }

    [Fact]
    public async Task AuthenticatedTripsEndpoint_SameBurstSize_IsNeverRateLimited()
    {
        // No [EnableRateLimiting] is attached to TripsController (and no global
        // DefaultRateLimiterPolicy exists in Program.cs), so this authenticated endpoint must
        // never see a 429 from the public-only policy, no matter how many requests the same
        // caller/IP sends.
        //
        // Posts to Create with an empty Stops list — same "fewer than 2 Stops -> FluentValidation
        // 400, never touches the DB" shortcut as
        // TripsControllerAuthorizationTests.Create_AllowedRole_IsNotRejectedAtAuthLayer.
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(Role.Planificador));
        var body = new
        {
            VehicleId = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
            Price = 15000m,
            Stops = Array.Empty<object>()
        };

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < BurstCount; i++)
        {
            var response = await client.PostAsJsonAsync(AuthenticatedTripsPath, body, cancellationToken);
            statuses.Add(response.StatusCode);
        }

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, statuses);
        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.BadRequest, status));
    }
}
