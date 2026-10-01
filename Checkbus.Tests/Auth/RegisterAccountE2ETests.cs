using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Checkbus.Tests.Auth;

/// <summary>
/// The single cross-slice Aspire end-to-end scenario for register-account (design D-13,
/// task 5.2). <see cref="Checkbus.Tests.Infrastructure.CheckbusApiFactory"/> runs with a
/// dummy connection string and never reaches a database, so this is the only harness that
/// can prove the full round trip: the 201-registration happy path (deferred from
/// <c>UsersControllerAuthorizationTests</c>), DNI-as-initial-password login, the
/// forced-change bypass (a user with <c>MustChangePassword = true</c> can still call an
/// ordinary role-gated endpoint), the self-service change-password 204 happy path
/// (deferred from <c>ChangePasswordTests</c>), and the flag clearing end to end.
/// </summary>
public class RegisterAccountE2ETests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    // Matches Checkbus.ApiService.Infrastructure.Context.CheckbusDbSeeder's seeded admin user.
    private const string SeededAdminEmail = "admin@checkbus.dev";
    private const string SeededAdminPassword = "Admin123!";

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

    private static async Task<(JsonElement Body, string Token)> LoginAsync(
        HttpClient client, string email, string password, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { Email = email, Password = password }, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = JsonDocument.Parse(json).RootElement;
        return (root, root.GetProperty("token").GetString()!);
    }

    private static void SetBearer(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task RegisterLoginChangePassword_FullRoundTrip_Succeeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var httpClient = await CreateApiClientAsync(cancellationToken);

        // 0. Regression guard, end to end: the seeded admin reports mustChangePassword:
        //    false (the CLR-default proven at the entity level in task 4.4; here as a
        //    full HTTP round trip through a real database).
        var (adminLogin, adminToken) = await LoginAsync(
            httpClient, SeededAdminEmail, SeededAdminPassword, cancellationToken);
        Assert.False(adminLogin.GetProperty("mustChangePassword").GetBoolean());
        var organizationId = adminLogin.GetProperty("organizationId").GetGuid();

        // 1. Register "José Díaz" as an Administrador in checkbus-demo — the 201 happy
        //    path CheckbusApiFactory cannot reach (D-13).
        SetBearer(httpClient, adminToken);
        const string documentNumber = "30999888";
        var registerResponse = await httpClient.PostAsJsonAsync(
            "/api/users",
            new
            {
                Name = "José",
                Surname = "Díaz",
                DocumentType = DocumentType.DNI,
                DocumentNumber = documentNumber,
                Role = Role.Administrador
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registerBody = JsonDocument.Parse(
            await registerResponse.Content.ReadAsStringAsync(cancellationToken)).RootElement;
        Assert.Equal("jose.diaz@checkbus-demo.com", registerBody.GetProperty("email").GetString());
        Assert.Equal(organizationId, registerBody.GetProperty("organizationId").GetGuid());
        Assert.True(registerBody.GetProperty("mustChangePassword").GetBoolean());

        // 2. Log in as the new user with the generated email and DocumentNumber as the
        //    initial password ("DNI as Initial Password").
        var (firstLogin, firstToken) = await LoginAsync(
            httpClient, "jose.diaz@checkbus-demo.com", documentNumber, cancellationToken);
        Assert.True(firstLogin.GetProperty("mustChangePassword").GetBoolean());

        // 3. The issued JWT is full and normally-scoped: it authorizes a call to an
        //    ordinary role-gated endpoint (POST api/users is Administrador-only) despite
        //    MustChangePassword being true. Proves "Login Is Never Blocked by
        //    MustChangePassword" and the forced-change-bypass anti-IDOR row — by explicit
        //    design, not by omission.
        SetBearer(httpClient, firstToken);
        var secondRegisterResponse = await httpClient.PostAsJsonAsync(
            "/api/users",
            new
            {
                Name = "Second",
                Surname = "User",
                DocumentType = DocumentType.DNI,
                DocumentNumber = "30999889",
                Role = Role.Chofer
            },
            cancellationToken);
        Assert.Equal(HttpStatusCode.Created, secondRegisterResponse.StatusCode);

        // 4. Self-service change-password — the 204 happy path ChangePasswordTests could
        //    not reach either (same D-13 constraint).
        const string newPassword = "NewSecurePassword1";
        var changeResponse = await httpClient.PostAsJsonAsync(
            "/api/auth/change-password", new { NewPassword = newPassword }, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, changeResponse.StatusCode);

        // 5. The next login reports mustChangePassword: false and the new password works.
        var (secondLogin, _) = await LoginAsync(
            httpClient, "jose.diaz@checkbus-demo.com", newPassword, cancellationToken);
        Assert.False(secondLogin.GetProperty("mustChangePassword").GetBoolean());

        // 6. The old DNI-derived password no longer works.
        var oldPasswordResponse = await httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "jose.diaz@checkbus-demo.com", Password = documentNumber },
            cancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordResponse.StatusCode);
    }
}
