using Checkbus.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Checkbus.Web.Components.Maps;

/// <summary>
/// Reusable Google Maps location-picker. Pure UI building block: it embeds a draggable map plus
/// a Places search box and resolves every pick (map click, marker drag, search selection) into a
/// <see cref="PickedLocation"/> via reverse geocoding — it makes no backend HTTP calls of its own.
/// Two modes:
/// <list type="bullet">
/// <item><see cref="LocationPickerMode.Single"/> — one marker, reports through <see cref="OnLocationPicked"/>.</item>
/// <item><see cref="LocationPickerMode.MultiStop"/> — an ordered list of stops (add via map/search,
/// remove/reorder via the buttons rendered below the map), reports the whole list through
/// <see cref="OnStopsChanged"/> on every change.</item>
/// </list>
/// </summary>
public partial class LocationPicker : ComponentBase, IAsyncDisposable
{
    private const string ModuleImportPath = "./Components/Maps/LocationPicker.razor.js";

    [Parameter]
    public LocationPickerMode Mode { get; set; } = LocationPickerMode.Single;

    [Parameter]
    public EventCallback<PickedLocation> OnLocationPicked { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<PickedLocation>> OnStopsChanged { get; set; }

    /// <summary>Optional pre-existing stops/location to seed the map with (e.g. re-rendering an
    /// already-built route). Only read once, at first render.</summary>
    [Parameter]
    public IReadOnlyList<PickedLocation>? InitialStops { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private GoogleMapsBrowserOptions GoogleMapsBrowserOptions { get; set; } = default!;

    private readonly string _mapElementId = $"location-picker-map-{Guid.NewGuid():N}";
    private readonly string _searchInputId = $"location-picker-search-{Guid.NewGuid():N}";
    private readonly List<PickedLocation> _stops = [];

    private IJSObjectReference? _module;
    private DotNetObjectReference<LocationPicker>? _dotNetRef;
    private bool _initialized;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _initialized)
            return;

        _initialized = true;

        if (InitialStops is { Count: > 0 })
            _stops.AddRange(InitialStops);

        _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModuleImportPath);
        _dotNetRef = DotNetObjectReference.Create(this);

        var modeToken = Mode == LocationPickerMode.MultiStop ? "multi" : "single";

        await _module.InvokeVoidAsync(
            "initMap",
            _mapElementId,
            _searchInputId,
            GoogleMapsBrowserOptions.ApiKey,
            modeToken,
            _stops,
            _dotNetRef);
    }

    /// <summary>Invoked from JS (single mode) whenever a location is picked or dragged.</summary>
    [JSInvokable]
    public async Task OnLocationPickedFromJs(PickedLocation location)
    {
        await OnLocationPicked.InvokeAsync(location);
    }

    /// <summary>Invoked from JS (multi-stop mode) whenever a new stop is added via the map/search.</summary>
    [JSInvokable]
    public async Task OnStopsUpdated(PickedLocation[] stops)
    {
        _stops.Clear();
        _stops.AddRange(stops);
        await InvokeAsync(StateHasChanged);
        await OnStopsChanged.InvokeAsync(_stops.AsReadOnly());
    }

    private async Task RemoveStopAsync(int index)
    {
        _stops.RemoveAt(index);
        await SyncStopsWithMapAsync();
    }

    private async Task MoveStopUpAsync(int index)
    {
        if (index <= 0)
            return;

        (_stops[index - 1], _stops[index]) = (_stops[index], _stops[index - 1]);
        await SyncStopsWithMapAsync();
    }

    private async Task MoveStopDownAsync(int index)
    {
        if (index >= _stops.Count - 1)
            return;

        (_stops[index + 1], _stops[index]) = (_stops[index], _stops[index + 1]);
        await SyncStopsWithMapAsync();
    }

    private async Task SyncStopsWithMapAsync()
    {
        if (_module is not null)
            await _module.InvokeVoidAsync("syncStops", _mapElementId, _stops);

        await OnStopsChanged.InvokeAsync(_stops.AsReadOnly());
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();

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
