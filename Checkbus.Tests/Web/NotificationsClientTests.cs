using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Features.Notifications.Contracts;
using Checkbus.Web.Features.Notifications.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="NotificationsClient"/> maps every realistic <c>api/Notifications/*</c>
/// server response (success, validation failure, not found, auth failure, transport failure)
/// into the matching closed outcome case. Mirrors <see cref="TripsClientTests"/>'s convention.
/// </summary>
public class NotificationsClientTests
{
    private static readonly Guid TripId = Guid.NewGuid();

    private static NotificationsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new NotificationsClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static CreateNotificationRequest MakeCreateRequest() => new()
    {
        TargetType = WebNotificationTargetType.Trip,
        TargetId = TripId,
        Reason = WebNotificationReason.CambioHorario,
        Message = "El viaje sale 30 minutos más tarde."
    };

    private static NotificationResponse MakeNotification() => new()
    {
        Id = Guid.NewGuid(),
        TargetType = WebNotificationTargetType.Trip,
        TargetId = TripId,
        Reason = WebNotificationReason.CambioHorario,
        Message = "El viaje sale 30 minutos más tarde.",
        CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        CreatedByName = "Juan Perez"
    };

    // ----- CreateAsync -----

    [Fact]
    public async Task CreateAsync_WhenCreated_ReturnsSuccess()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<NotificationCreateOutcome.Success>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["Message"] = ["'Message' must not be empty."]
            }))
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var validationFailed = Assert.IsType<NotificationCreateOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("Message", error.Key);
    }

    [Fact]
    public async Task CreateAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<NotificationCreateOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<NotificationCreateOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<NotificationCreateOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenTransportFails_ReturnsTransportError()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<NotificationCreateOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }

    [Fact]
    public async Task CreateAsync_SendsPostToNotifications()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created));
        var client = CreateClient(stub);

        await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Post, stub.CapturedRequest!.Method);
        Assert.Equal("Notifications", stub.CapturedRequest.RequestUri!.AbsolutePath.TrimStart('/').Split('/').Last());
    }

    // ----- GetTripHistoryAsync -----

    [Fact]
    public async Task GetTripHistoryAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new[] { MakeNotification() };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetTripHistoryAsync(TripId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<NotificationHistoryOutcome.Success>(outcome);
        var notification = Assert.Single(success.Notifications);
        Assert.Equal(TripId, notification.TargetId);
        Assert.Equal("Juan Perez", notification.CreatedByName);
    }

    [Fact]
    public async Task GetTripHistoryAsync_WhenOkWithEmptyList_ReturnsSuccessWithEmptyList()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<NotificationResponse>())
        });
        var client = CreateClient(stub);

        var outcome = await client.GetTripHistoryAsync(TripId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<NotificationHistoryOutcome.Success>(outcome);
        Assert.Empty(success.Notifications);
    }

    [Fact]
    public async Task GetTripHistoryAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetTripHistoryAsync(TripId, TestContext.Current.CancellationToken);

        Assert.IsType<NotificationHistoryOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetTripHistoryAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetTripHistoryAsync(TripId, TestContext.Current.CancellationToken);

        Assert.IsType<NotificationHistoryOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetTripHistoryAsync_WhenTransportFails_ReturnsTransportError()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetTripHistoryAsync(TripId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<NotificationHistoryOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }

    [Fact]
    public async Task GetTripHistoryAsync_SendsGetWithTripIdQueryString()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<NotificationResponse>())
        });
        var client = CreateClient(stub);

        await client.GetTripHistoryAsync(TripId, TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains($"tripId={TripId}", stub.CapturedRequest.RequestUri!.Query);
    }
}
