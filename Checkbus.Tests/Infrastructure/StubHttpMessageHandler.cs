namespace Checkbus.Tests.Infrastructure;

/// <summary>
/// Test double <see cref="HttpMessageHandler"/> that captures the outgoing
/// <see cref="HttpRequestMessage"/> for header/content assertions and returns a canned
/// <see cref="HttpResponseMessage"/> without making any real network call.
/// </summary>
internal sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    public HttpRequestMessage? CapturedRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CapturedRequest = request;
        return Task.FromResult(response);
    }
}
