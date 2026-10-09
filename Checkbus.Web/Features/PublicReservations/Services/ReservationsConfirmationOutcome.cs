using Checkbus.Web.Features.PublicReservations.Contracts;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/public/reservations</c> call
/// (odd/tasks/reservas-publicas.md R5). Mirrors <see cref="PublicTripSeatMapOutcome"/>'s shape
/// (no ValidationFailed case — this is a parameterless-body GET, nothing for FluentValidation
/// to reject).
/// </summary>
public abstract record ReservationsConfirmationOutcome
{
    private ReservationsConfirmationOutcome()
    {
    }

    /// <summary>200 OK — at least one submitted Reservation id resolved.</summary>
    public sealed record Success(ReservationsConfirmationResponse Confirmation) : ReservationsConfirmationOutcome;

    /// <summary>404 Not Found — none of the submitted Reservation ids resolved.</summary>
    public sealed record NotFound : ReservationsConfirmationOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : ReservationsConfirmationOutcome;
}
