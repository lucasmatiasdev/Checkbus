using Checkbus.Web.Features.Users.Contracts;

namespace Checkbus.Web.Features.Boarding.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Queries.TripBoardingListDto</c>, the response body
/// for <c>GET /api/boarding/{tripId}</c>. Deliberately excludes Email/Phone, same as the API DTO
/// it mirrors.
/// </summary>
public sealed record TripBoardingListResponse
{
    public required IReadOnlyList<BoardingPassengerResponse> Passengers { get; init; }
}

public sealed record BoardingPassengerResponse
{
    public required Guid ReservationId { get; init; }
    public int SeatNumber { get; init; }
    public required string PassengerName { get; init; }
    public required string PassengerSurname { get; init; }
    public WebDocumentType PassengerDocumentType { get; init; }
    public required string PassengerDocumentNumber { get; init; }
    public WebBoardingStatus Status { get; init; }
}
