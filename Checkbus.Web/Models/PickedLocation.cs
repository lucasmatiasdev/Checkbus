namespace Checkbus.Web.Models;

/// <summary>
/// A single location picked through the <c>LocationPicker</c> component — either via a map
/// click/drag, a Places search selection, reverse-geocoded in both cases. Field names intentionally
/// match the eventual <c>Location</c> entity (<c>Nombre</c>, <c>Direccion</c>, <c>PlaceId</c>,
/// <c>Latitud</c>, <c>Longitud</c>) so the R4/R5 forms that consume this can map it directly with
/// no renaming.
/// </summary>
/// <remarks>
/// Property names are also relied upon by JS interop: <c>LocationPicker.razor.js</c> sends plain
/// JS objects with camelCase keys (<c>nombre</c>, <c>direccion</c>, <c>placeId</c>, <c>latitud</c>,
/// <c>longitud</c>) that Blazor's JSON options deserialize case-insensitively into this record's
/// constructor parameters.
/// </remarks>
public sealed record PickedLocation(string Nombre, string Direccion, string PlaceId, double Latitud, double Longitud);
