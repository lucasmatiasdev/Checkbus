namespace Checkbus.Web.Models;

/// <summary>
/// Google Maps browser-exposed API key, used client-side by <c>LocationPicker</c> to load the
/// Maps JavaScript API (and its Places/Geocoding capabilities) in the user's browser.
///
/// Deliberately a *separate* key from <c>Checkbus.ApiService</c>'s
/// <c>GoogleMapsOptions.ApiKey</c> (the server-side Directions API key, which must stay
/// IP-restricted and must never reach the browser). This key is the kind Google expects to be
/// publicly visible in page source — it ends up in a <c>&lt;script src="...?key=..."&gt;</c> tag
/// the browser renders — and should instead be restricted by HTTP referrer in Google Cloud
/// Console.
///
/// Binds permissively like its ApiService counterpart (<see cref="ApiKey"/> defaults to an empty
/// string, no throw-if-missing): no key has been provisioned yet, so a missing "GoogleMaps"
/// config section or empty key must not fail Checkbus.Web startup. The real Maps script load
/// only fails client-side (browser console, not a .NET exception) once the browser actually
/// requests the script with no usable key.
/// </summary>
public class GoogleMapsBrowserOptions
{
    public string ApiKey { get; set; } = string.Empty;
}
