// Isolated JS module for the LocationPicker Blazor component.
//
// This establishes Checkbus's first JS-interop convention: a colocated `.razor.js` module next
// to its `.razor` component, loaded by the component via IJSRuntime as an IJSObjectReference, and
// exporting plain functions invoked from .NET. Multiple LocationPicker instances can exist on the
// same page (e.g. a single-pick map for an Evento and a multi-stop map for a Viaje); each is
// tracked independently by its DOM element id in the `instances` map below, but the Google Maps
// <script> tag itself is injected into the page at most once — `googleMapsLoadPromise` is a
// module-level (shared across every instance) promise that every `initMap` call awaits, instead
// of appending a second <script> tag on a second instance.

const instances = new Map();
let googleMapsLoadPromise = null;

function loadGoogleMaps(apiKey) {
    if (googleMapsLoadPromise) {
        return googleMapsLoadPromise;
    }

    googleMapsLoadPromise = new Promise((resolve, reject) => {
        if (window.google && window.google.maps && window.google.maps.places) {
            resolve(window.google.maps);
            return;
        }

        const script = document.createElement("script");
        script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&libraries=places`;
        script.async = true;
        script.defer = true;
        script.onload = () => resolve(window.google.maps);
        script.onerror = () => reject(new Error("LocationPicker: failed to load the Google Maps JavaScript API."));
        document.head.appendChild(script);
    });

    return googleMapsLoadPromise;
}

function reverseGeocode(geocoder, latLng) {
    return new Promise((resolve) => {
        geocoder.geocode({ location: latLng }, (results, status) => {
            if (status === "OK" && results && results.length > 0) {
                resolve(results[0]);
            } else {
                resolve(null);
            }
        });
    });
}

function toPickedLocation(nombre, direccion, placeId, latLng) {
    return {
        nombre: nombre || direccion || "",
        direccion: direccion || "",
        placeId: placeId || "",
        latitud: latLng.lat(),
        longitud: latLng.lng(),
    };
}

// Resolves a human-readable address/placeId for a point that only has coordinates so far (a map
// click or a marker drag), via reverse geocoding. A Places search selection already carries both,
// so it skips straight through.
async function resolveLocation(instance, latLng, nombre, direccion, placeId) {
    if (!direccion || !placeId) {
        const geocoded = await reverseGeocode(instance.geocoder, latLng);
        direccion = direccion || (geocoded ? geocoded.formatted_address : "");
        placeId = placeId || (geocoded ? geocoded.place_id : "");
    }

    return toPickedLocation(nombre, direccion, placeId, latLng);
}

async function placeSingleMarker(instance, latLng, nombre, direccion, placeId) {
    const maps = instance.maps;

    if (instance.marker) {
        instance.marker.setPosition(latLng);
    } else {
        instance.marker = new maps.Marker({
            position: latLng,
            map: instance.map,
            draggable: true,
        });
        instance.marker.addListener("dragend", () => {
            placeSingleMarker(instance, instance.marker.getPosition(), null, null, null);
        });
    }

    instance.map.panTo(latLng);

    const picked = await resolveLocation(instance, latLng, nombre, direccion, placeId);
    await instance.dotNetRef.invokeMethodAsync("OnLocationPicked", picked);
}

async function addStopMarker(instance, latLng, nombre, direccion, placeId) {
    const maps = instance.maps;
    const marker = new maps.Marker({
        position: latLng,
        map: instance.map,
        label: `${instance.markers.length + 1}`,
    });
    instance.markers.push(marker);
    instance.map.panTo(latLng);

    const picked = await resolveLocation(instance, latLng, nombre, direccion, placeId);
    instance.stops.push(picked);

    await instance.dotNetRef.invokeMethodAsync("OnStopsUpdated", instance.stops);
}

function renderStopMarkers(instance) {
    instance.markers.forEach((marker) => marker.setMap(null));
    instance.markers = [];

    instance.stops.forEach((stop, index) => {
        const position = new instance.maps.LatLng(stop.latitud, stop.longitud);
        const marker = new instance.maps.Marker({ position, map: instance.map, label: `${index + 1}` });
        instance.markers.push(marker);
    });
}

function bindPlacesAutocomplete(instance, searchInputId, mode) {
    const searchInput = document.getElementById(searchInputId);
    if (!searchInput) {
        return null;
    }

    const autocomplete = new instance.maps.places.Autocomplete(searchInput);
    autocomplete.bindTo("bounds", instance.map);

    const listener = autocomplete.addListener("place_changed", () => {
        const place = autocomplete.getPlace();
        if (!place || !place.geometry || !place.geometry.location) {
            return;
        }

        const latLng = place.geometry.location;
        const nombre = place.name || null;
        const direccion = place.formatted_address || null;
        const placeId = place.place_id || null;

        if (mode === "multi") {
            addStopMarker(instance, latLng, nombre, direccion, placeId);
        } else {
            placeSingleMarker(instance, latLng, nombre, direccion, placeId);
        }
    });

    return listener;
}

// Buenos Aires — reasonable fallback center until a marker/stop is placed.
const DEFAULT_CENTER = { lat: -34.6037, lng: -58.3816 };

export async function initMap(elementId, searchInputId, apiKey, mode, initialStops, dotNetRef) {
    const maps = await loadGoogleMaps(apiKey);

    const element = document.getElementById(elementId);
    if (!element) {
        throw new Error(`LocationPicker: element "${elementId}" not found.`);
    }

    const map = new maps.Map(element, {
        center: DEFAULT_CENTER,
        zoom: 13,
    });

    const instance = {
        map,
        maps,
        geocoder: new maps.Geocoder(),
        dotNetRef,
        mode,
        marker: null,
        markers: [],
        stops: Array.isArray(initialStops) ? [...initialStops] : [],
    };

    instances.set(elementId, instance);

    const clickListener = map.addListener("click", (event) => {
        if (mode === "multi") {
            addStopMarker(instance, event.latLng, null, null, null);
        } else {
            placeSingleMarker(instance, event.latLng, null, null, null);
        }
    });
    instance.clickListener = clickListener;

    instance.autocompleteListener = bindPlacesAutocomplete(instance, searchInputId, mode);

    if (mode === "multi" && instance.stops.length > 0) {
        renderStopMarkers(instance);
        const first = instance.stops[0];
        map.panTo(new maps.LatLng(first.latitud, first.longitud));
    } else if (mode === "single" && instance.stops.length > 0) {
        const only = instance.stops[0];
        const latLng = new maps.LatLng(only.latitud, only.longitud);
        instance.marker = new maps.Marker({ position: latLng, map, draggable: true });
        instance.marker.addListener("dragend", () => {
            placeSingleMarker(instance, instance.marker.getPosition(), null, null, null);
        });
        map.panTo(latLng);
    }
}

// Re-renders multi-stop markers from a .NET-owned ordered list — called after the Razor UI
// removes or reorders a stop (plain up/down buttons, no drag-and-drop), since that mutation
// happens entirely on the .NET side and the map needs to catch up.
export function syncStops(elementId, stops) {
    const instance = instances.get(elementId);
    if (!instance) {
        return;
    }

    instance.stops = Array.isArray(stops) ? [...stops] : [];
    renderStopMarkers(instance);
}

export function dispose(elementId) {
    const instance = instances.get(elementId);
    if (!instance) {
        return;
    }

    if (instance.clickListener) {
        instance.clickListener.remove();
    }
    if (instance.autocompleteListener) {
        instance.autocompleteListener.remove();
    }
    if (instance.marker) {
        instance.marker.setMap(null);
    }
    instance.markers.forEach((marker) => marker.setMap(null));

    instances.delete(elementId);
}
