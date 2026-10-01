using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Checkbus.Tests.Auth;

public class AuthLoginEndpointTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Matches Checkbus.ApiService.Infrastructure.Context.CheckbusDbSeeder's seeded admin user.
    private const string SeededEmail = "admin@checkbus.dev";
    private const string SeededPassword = "Admin123!";

    private static async Task<HttpClient> CreateApiClientAsync(CancellationToken cancellationToken)
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Checkbus_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        return app.CreateHttpClient("apiservice");
    }

    [Fact]
    public async Task Login_MalformedEmail_Returns400WithFieldError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var httpClient = await CreateApiClientAsync(cancellationToken);

        var response = await httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "not-an-email", Password = "password123" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("Email", out _));
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400WithFieldError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var httpClient = await CreateApiClientAsync(cancellationToken);

        var response = await httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "admin@checkbus.dev", Password = "" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task Login_ShortNonEmptyPassword_Returns401NotValidationError()
    {
        // The login validator no longer enforces a minimum password length (a
        // DNI-derived initial password may legitimately be 7 characters). A short
        // password now reaches IPasswordHasher.Verify and fails authentication
        // instead of being rejected by FluentValidation.
        var cancellationToken = TestContext.Current.CancellationToken;
        var httpClient = await CreateApiClientAsync(cancellationToken);

        var response = await httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "admin@checkbus.dev", Password = "short" },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_ReturnByteIdentical401()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var httpClient = await CreateApiClientAsync(cancellationToken);

        var unknownResponse = await httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "nobody@example.com", Password = "password123" },
            cancellationToken);
        var unknownBody = await unknownResponse.Content.ReadAsStringAsync(cancellationToken);

        var wrongPasswordResponse = await httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = SeededEmail, Password = "wrong-password" },
            cancellationToken);
        var wrongPasswordBody = await wrongPasswordResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, unknownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(unknownBody, wrongPasswordBody);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokenAndUserFields()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var httpClient = await CreateApiClientAsync(cancellationToken);

        var response = await httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = SeededEmail, Password = SeededPassword },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("token").GetString()));
        Assert.True(root.TryGetProperty("userId", out _));
        Assert.True(root.TryGetProperty("organizationId", out _));
        Assert.Equal("Administrador", root.GetProperty("role").GetString());
        Assert.Equal("admin@checkbus.dev", root.GetProperty("email").GetString());
        Assert.False(root.GetProperty("mustChangePassword").GetBoolean());
    }
}
