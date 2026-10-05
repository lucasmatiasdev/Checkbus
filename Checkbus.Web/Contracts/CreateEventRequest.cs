namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Events.Commands.CreateEventCommand</c>, the request body
/// for <c>POST /api/Events</c>. The location fields are flattened to match
/// <c>Checkbus.Web.Models.PickedLocation</c>'s shape exactly, so a page can map one straight into
/// the other with no renaming (<c>EventNew.razor</c> does this with R3's <c>LocationPicker</c>
/// in <c>LocationPickerMode.Single</c> mode).
/// </summary>
public sealed record CreateEventRequest
{
    public required string Name { get; init; }
    public required WebEventType Type { get; init; }
    public required DateTime Date { get; init; }
    public required string LocationName { get; init; }
    public required string Address { get; init; }
    public required string PlaceId { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
}
