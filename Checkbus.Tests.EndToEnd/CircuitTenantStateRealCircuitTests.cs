using Microsoft.Playwright;

namespace Checkbus.Tests.EndToEnd;

/// <summary>
/// S3.8 — proves <c>CircuitTenantState</c>'s fallback (decision #23) actually
/// works inside a GENUINE Blazor Server interactive circuit: a real Kestrel
/// listener, a real browser (Playwright/Chromium) executing
/// <c>blazor.web.js</c>, a real prerender-then-hydrate two-pass render, and a
/// real SignalR WebSocket handshake — none of which a mocked
/// <c>HttpContext</c> (see <c>HttpContextTenantProviderTests</c>) can
/// reproduce, because a fake <c>HttpContext</c> is always "available" by
/// construction and can never exercise the failure mode
/// (<c>IHttpContextAccessor.HttpContext == null</c> mid-circuit) the original
/// design decision was worried about.
///
/// Flow: seed a real Organization+Administrador User directly in the test
/// database → launch headless Chromium → sign in through the real
/// <c>/account/login</c> form (real cookie, real antiforgery token rendered
/// by <c>&lt;AntiforgeryToken /&gt;</c>, both handled naturally by the
/// browser — no manual HTTP/cookie plumbing) → navigate to the real
/// <c>MainLayout</c>-wrapped <c>/app/drivers/new</c> page and let the circuit
/// actually establish → submit the driver form through that live circuit
/// (typing into real DOM inputs and clicking the real submit button, never
/// calling <c>IFleetService</c> directly) → assert the created
/// <c>Driver</c> row's <c>OrganizationId</c> matches the authenticated
/// admin's real organization.
/// </summary>
public class CircuitTenantStateRealCircuitTests : IAsyncLifetime
{
    private CheckbusWebAppFactory _factory = null!;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        _factory = new CheckbusWebAppFactory();
        // Accessing Services forces WebApplicationFactory to build and start
        // the real Kestrel host (see CreateHost override), so
        // _factory.ServerAddress is populated before any test body runs.
        _ = _factory.Services;

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Driver_Created_Through_A_Real_Interactive_Circuit_Is_Stamped_With_The_Authenticated_Users_Organization()
    {
        var (organizationId, email, password) = await TestDataSeeder.SeedOrganizationWithAdminAsync(_factory);

        await using var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // 1) Real sign-in through the real SSR form + real antiforgery token.
        // NOTE: WaitUntilState.NetworkIdle is deliberately NOT used anywhere in
        // this test. Blazor Server keeps a persistent SignalR WebSocket open,
        // so the network never truly goes idle — waiting on NetworkIdle here
        // was found to be flaky (intermittent 15s timeouts, ~1-in-3 runs)
        // precisely because of that open connection, not because of any bug
        // in CircuitTenantState. DOMContentLoaded plus waiting for a concrete,
        // meaningful DOM element (the real hydration signal) is the reliable
        // pattern for SignalR-backed pages.
        await page.GotoAsync($"{_factory.ServerAddress}/login", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForURLAsync(url => url.EndsWith("/app"), new PageWaitForURLOptions { Timeout = 30000 });

        // 2) Navigate to a real MainLayout-wrapped interactive page and let
        // the circuit actually establish (prerender, then blazor.web.js
        // opens the SignalR WebSocket and hydrates). The MudBlazor form only
        // exists in the DOM once hydration completes, so waiting for the
        // "Guardar" button to be actionable is itself proof the real circuit
        // came up — a fake HttpContext test cannot fail this way.
        await page.GotoAsync($"{_factory.ServerAddress}/app/drivers/new", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var saveButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Guardar" });
        await saveButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });

        // 3) Submit the form through the LIVE circuit (real DOM interaction,
        // never calling IFleetService/FleetService directly). LicenseCategory
        // and license expiry keep their pre-set defaults (B1 / +1 year), so
        // only the two required text fields need filling.
        const string fullName = "E2E Circuit Driver";
        await page.GetByLabel("Nombre completo").FillAsync(fullName);
        await page.GetByLabel("DNI").FillAsync("30111222");
        await saveButton.ClickAsync();

        // Successful save navigates back to /app (see DriverCreate.razor's
        // SaveAsync -> NavigationManager.NavigateTo("/app")).
        await page.WaitForURLAsync(url => url.EndsWith("/app"), new PageWaitForURLOptions { Timeout = 30000 });

        // 4) The real proof: the Driver row exists and carries the
        // authenticated admin's real OrganizationId, resolved entirely
        // through CircuitTenantState's fallback inside the live circuit
        // (DriverCreate.razor reads CircuitTenantState.OrganizationId, never
        // HttpContext, once past the initial SSR render).
        var driver = await TestDataSeeder.FindDriverByFullNameAsync(_factory, fullName);
        Assert.NotNull(driver);
        Assert.Equal(organizationId, driver!.OrganizationId);
        Assert.Equal("30111222", driver.Dni);
    }
}
