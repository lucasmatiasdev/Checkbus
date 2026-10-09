using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Features.PublicReservations.Contracts;
using Checkbus.Web.Features.PublicReservations.Services;
using Checkbus.Web.Features.Users.Contracts;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="PublicReservationsClient"/> maps every realistic
/// <c>api/public/reservations</c> server response (success, not found, transport failure)
/// into the matching closed outcome case. Mirrors <see cref="PublicTripsClientTests"/>'s
/// convention. No ValidationFailed/Forbidden case to test — this is a parameterless-body,
/// anonymous GET.
/// </summary>
public class PublicReservationsClientTests
{
    private static PublicReservationsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new PublicReservationsClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static ReservationsConfirmationResponse MakeConfirmation() => new()
    {
        Reservations =
        [
            new ReservationConfirmation
            {
                ReservationId = Guid.NewGuid(),
                SeatNumber = 7,
                PassengerName = "Juan",
                PassengerSurname = "Perez",
                PassengerDocumentType = WebDocumentType.DNI,
                PassengerDocumentNumber = "30111222",
                Price = 15000m,
                Status = "Confirmada",
                EventName = "Cosquin Rock",
                DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc)
            }
        ]
    };

    [Fact]
    public async Task GetConfirmationAsync_WhenOk_ReturnsSuccessWithDeserializedConfirmation()
    {
        var expected = MakeConfirmation();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetConfirmationAsync([expected.Reservations[0].ReservationId], TestContext.Current.CancellationToken);

        var success = Assert.IsType<ReservationsConfirmationOutcome.Success>(outcome);
        Assert.Single(success.Confirmation.Reservations);
        Assert.Equal(7, success.Confirmation.Reservations[0].SeatNumber);
    }

    [Fact]
    public async Task GetConfirmationAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetConfirmationAsync([Guid.NewGuid()], TestContext.Current.CancellationToken);

        Assert.IsType<ReservationsConfirmationOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetConfirmationAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetConfirmationAsync([Guid.NewGuid()], TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<ReservationsConfirmationOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetConfirmationAsync_GetsExpectedRouteWithRepeatedIdsKeys()
    {
        // ASP.NET Core model-binds IReadOnlyList<Guid> from repeated "ids=" query keys, not a
        // single comma-joined value — a comma-joined "ids=a,b" fails binding with a 400 (found
        // during R7 manual smoke testing).
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(MakeConfirmation())
        });
        var client = CreateClient(stub);

        await client.GetConfirmationAsync([id1, id2], TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains($"public/reservations?ids={id1}&ids={id2}", stub.CapturedRequest.RequestUri!.ToString());
    }
}
