using Checkbus.Web.Features.Trips.Contracts;

namespace Checkbus.Web.Features.PublicReservations.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Queries.PublicTripDetailDto</c>, the response body
/// for <c>GET /api/public/trips/{id}/details</c> (odd/tasks/detalle-reserva-publica.md T2).
/// Checkbus.Web does not reference the API projects (pure BFF), so this DTO is duplicated here
/// rather than shared — same convention as <see cref="PublicTripSeatMapResponse"/>. The Stop's
/// Type reuses the existing <see cref="WebStopType"/> mirror from the Trips feature (same
/// reasoning <c>PublicReservationsClient</c> already used to reuse <c>WebDocumentType</c> rather
/// than inventing a duplicate enum).
/// </summary>
public sealed record PublicTripDetailResponse
{
    public required Guid TripId { get; init; }
    public required string OrganizationName { get; init; }
    public required Guid EventId { get; init; }
    public required string EventName { get; init; }

    // Mirrors EventResponse.HasImage — never the raw storage key. When true, the page fetches
    // GET api/public/trips/{TripId}/event-image (T1's GET api/Events/{EventId}/image is
    // Planificador/Administrador-only and cannot serve an anonymous passenger — see T4's
    // GetPublicTripEventImageQueryHandler).
    public bool EventHasImage { get; init; }

    public decimal Price { get; init; }
    public int AvailableSeats { get; init; }
    public DateTime DepartureDate { get; init; }
    public DateTime ArrivalDate { get; init; }

    public required IReadOnlyList<PublicTripStop> Stops { get; init; }
}

public sealed record PublicTripStop
{
    public int Order { get; init; }
    public WebStopType Type { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}
