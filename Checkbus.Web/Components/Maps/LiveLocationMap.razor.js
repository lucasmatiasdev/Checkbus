// Isolated JS module for the LiveLocationMap Blazor component (DEMO-ONLY,
// odd/tasks/seguimiento-tiempo-real-demo.md T4).
//
// Clones LocationPicker.razor.js's Google Maps loading convention exactly: a colocated
// `.razor.js` module loaded by the component via IJSRuntime as an IJSObjectReference, exporting
// plain functions invoked from .NET, with the Google Maps <script> tag injected into the page at
// most once via a module-level `googleMapsLoadPromise` every `initMap` call awaits. Deliberately
// much simpler than LocationPicker though: no Places/Autocomplete, no click-to-add, no
// single/multi-stop modes — just one marker per instance that MOVES on every poll instead of
// being replaced. Each instance is tracked by its DOM element id in `instances`, same as
// LocationPicker.

const instances = new Map();
let googleMapsLoadPromise = null;

function loadGoogleMaps(apiKey) {
    if (googleMapsLoadPromise) {
        return googleMapsLoadPromise;
    }

    googleMapsLoadPromise = new Promise((resolve, reject) => {
        if (window.google && window.google.maps) {
            resolve(window.google.maps);
            return;
        }

        const script = document.createElement("script");
        script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}`;
        script.async = true;
        script.defer = true;
        script.onload = () => resolve(window.google.maps);
        script.onerror = () => reject(new Error("LiveLocationMap: failed to load the Google Maps JavaScript API."));
        document.head.appendChild(script);
    });

    return googleMapsLoadPromise;
}

export async function initMap(elementId, apiKey, latitude, longitude) {
    const maps = await loadGoogleMaps(apiKey);

    const element = document.getElementById(elementId);
    if (!element) {
        throw new Error(`LiveLocationMap: element "${elementId}" not found.`);
    }

    const center = { lat: latitude, lng: longitude };
    const map = new maps.Map(element, {
        center,
        zoom: 14,
    });

    const marker = new maps.Marker({ position: center, map });

    instances.set(elementId, { map, maps, marker });
}

// Moves the existing marker to the new position instead of adding a new one on every poll tick
// — the single-pin, last-known-position demo scope this component is built for (S4).
export function updateMarker(elementId, latitude, longitude) {
    const instance = instances.get(elementId);
    if (!instance) {
        return;
    }

    const position = new instance.maps.LatLng(latitude, longitude);
    instance.marker.setPosition(position);
    instance.map.panTo(position);
}

export function dispose(elementId) {
    const instance = instances.get(elementId);
    if (!instance) {
        return;
    }

    instance.marker.setMap(null);
    instances.delete(elementId);
}
