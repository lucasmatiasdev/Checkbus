using Checkbus.Web.Features.PublicReservations.Services;
using Checkbus.Web.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Checkbus.Web.Components.Maps;

/// <summary>
/// DEMO-ONLY (odd/tasks/seguimiento-tiempo-real-demo.md T4 — see S2 for why this is a deliberate,
/// bounded exception to the real "no se construye GPS real" decision, not a reopening of it).
/// Read-only Google Maps pin for the public passenger confirmation page: polls
/// <c>GET api/public/trips/{TripId}/location</c> (T2) on a .NET-side timer and moves a single
/// marker to the latest reported position.
///
/// Clones <see cref="LocationPicker"/>'s JS-loading convention exactly — same
/// <c>IJSRuntime.InvokeAsync&lt;IJSObjectReference&gt;("import", ...)</c>, the same
/// <see cref="GoogleMapsBrowserOptions.ApiKey"/> injection, the same "inject the
/// <c>&lt;script src="https://maps.googleapis.com/maps/api/js?key=..."&gt;</c> tag at most once"
/// pattern in the colocated <c>.razor.js</c> module — but deliberately much simpler: no
/// Autocomplete, no click-to-add, no single/multi-stop modes. Just one marker per instance that
/// MOVES on every poll instead of being replaced.
///
/// Renders nothing while <see cref="TripLocationResponse.LiveTrackingEnabled"/> is false (see the
/// .razor file's own comment) — consistent with FA2 of the use case ("sin mensaje adicional").
/// </summary>
public partial class LiveLocationMap : ComponentBase, IAsyncDisposable
{
    private const string ModuleImportPath = "./Components/Maps/LiveLocationMap.razor.js";

    // Demo-scale polling cadence (S4: last-known-position only, no trail/history) — frequent
    // enough to feel "live" for a short classroom demo without hammering the public endpoint.
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(7);

    [Parameter, EditorRequired]
    public Guid TripId { get; set; }

    [Inject]
    private PublicTripsClient PublicTripsClient { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private GoogleMapsBrowserOptions GoogleMapsBrowserOptions { get; set; } = default!;

    private readonly string _mapElementId = $"live-location-map-{Guid.NewGuid():N}";

    private IJSObjectReference? _module;
    private PeriodicTimer? _timer;
    private Task? _pollingLoopTask;
    private CancellationTokenSource? _cts;

    private bool _liveTrackingEnabled;
    private bool _mapInitialized;
    private double _latitude;
    private double _longitude;

    protected override async Task OnInitializedAsync()
    {
        // Poll once immediately so a trip that's already tracking shows its pin on first render,
        // instead of waiting a full PollInterval for the recurring loop below to kick in.
        await PollOnceAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_liveTrackingEnabled || _mapInitialized)
            return;

        _mapInitialized = true;

        _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModuleImportPath);
        await _module.InvokeVoidAsync(
            "initMap", _mapElementId, GoogleMapsBrowserOptions.ApiKey, _latitude, _longitude);

        StartPolling();
    }

    private void StartPolling()
    {
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(PollInterval);
        _pollingLoopTask = RunPollingLoopAsync(_cts.Token);
    }

    private async Task RunPollingLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(cancellationToken))
            {
                await PollOnceAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on DisposeAsync/navigation — not an error.
        }
    }

    private async Task PollOnceAsync()
    {
        var outcome = await PublicTripsClient.GetLocationAsync(TripId);

        if (outcome is not PublicTripLocationOutcome.Success success)
        {
            // Transport error on one poll: keep showing the last known state (if any) and try
            // again on the next tick rather than tearing the map down.
            return;
        }

        var location = success.Location;

        if (!location.LiveTrackingEnabled || location.Latitude is null || location.Longitude is null)
        {
            if (_liveTrackingEnabled)
            {
                _liveTrackingEnabled = false;
                await InvokeAsync(StateHasChanged);
            }

            return;
        }

        _latitude = location.Latitude.Value;
        _longitude = location.Longitude.Value;

        if (!_liveTrackingEnabled)
        {
            // First time we see tracking turn on: flip the flag and re-render so the <div> for
            // the map exists before OnAfterRenderAsync tries to initialize it.
            _liveTrackingEnabled = true;
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (_module is not null)
        {
            await _module.InvokeVoidAsync("updateMarker", _mapElementId, _latitude, _longitude);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();

        if (_pollingLoopTask is not null)
        {
            try
            {
                await _pollingLoopTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _timer?.Dispose();
        _cts?.Dispose();

        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose", _mapElementId);
            }
            catch (JSDisconnectedException)
            {
                // Circuit already torn down (e.g. the user navigated away) — nothing left to
                // clean up client-side.
            }

            await _module.DisposeAsync();
        }
    }
}
