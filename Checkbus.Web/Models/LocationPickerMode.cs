namespace Checkbus.Web.Models;

/// <summary>
/// Operating mode for the <c>LocationPicker</c> Blazor component.
/// </summary>
public enum LocationPickerMode
{
    /// <summary>Picks exactly one location (e.g. an <c>Event</c>'s venue).</summary>
    Single,

    /// <summary>Builds an ordered list of stops (e.g. a <c>Trip</c>'s route).</summary>
    MultiStop
}
