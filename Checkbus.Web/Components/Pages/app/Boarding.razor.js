// Isolated JS module for Boarding.razor's DEMO-ONLY live-tracking switch
// (odd/tasks/seguimiento-tiempo-real-demo.md). Follows the same colocated `.razor.js` convention
// established by LocationPicker.razor/.razor.js: loaded by the component via IJSRuntime as an
// IJSObjectReference, exporting plain functions invoked from .NET.
//
// Unlike LocationPicker (which can have multiple instances on a page), there is at most one
// Boarding screen — and therefore at most one driver-device watcher — open at a time, so this
// module keeps a single module-level watcher instead of an instance map.

let watchId = null;
let lastSentAtMs = 0;

function describeGeolocationError(error) {
    switch (error.code) {
        case error.PERMISSION_DENIED:
            return "Permiso de ubicación denegado por el navegador.";
        case error.POSITION_UNAVAILABLE:
            return "No se pudo determinar la ubicación del dispositivo.";
        case error.TIMEOUT:
            return "Se agotó el tiempo de espera al obtener la ubicación.";
        default:
            return "No se pudo obtener la ubicación del dispositivo.";
    }
}

// Starts watching the browser's Geolocation API and throttles how often a received position is
// relayed back to .NET (watchPosition can fire far more often than this demo's backend needs to
// be posted to) — minIntervalMs is enforced here, client-side, before ever invoking .NET.
export function startWatching(dotNetRef, minIntervalMs) {
    if (!("geolocation" in navigator)) {
        dotNetRef.invokeMethodAsync("OnGeolocationError", "Este navegador no soporta geolocalización.");
        return;
    }

    stopWatching();
    lastSentAtMs = 0;

    watchId = navigator.geolocation.watchPosition(
        (position) => {
            const now = Date.now();
            if (now - lastSentAtMs < minIntervalMs) {
                return;
            }
            lastSentAtMs = now;

            dotNetRef.invokeMethodAsync(
                "OnPositionReceived",
                position.coords.latitude,
                position.coords.longitude);
        },
        (error) => {
            dotNetRef.invokeMethodAsync("OnGeolocationError", describeGeolocationError(error));
        },
        { enableHighAccuracy: true, maximumAge: 0 });
}

// Idempotent — safe to call even when nothing is being watched (e.g. on component dispose after
// the switch was already off).
export function stopWatching() {
    if (watchId !== null) {
        navigator.geolocation.clearWatch(watchId);
        watchId = null;
    }
}
