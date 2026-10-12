namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/public/reservations/{id}/cancel</c> call
/// (odd/tasks/reservas-publicas.md Fase 4 / C5). Mirrors <see cref="CreateReservationOutcome"/>'s
/// shape — no ValidationFailed case, same reasoning as <see cref="ReservationsConfirmationOutcome"/>:
/// this is a parameterless-body POST (the id comes from the route), nothing for FluentValidation
/// to reject at the client level.
/// </summary>
public abstract record CancelReservationOutcome
{
    private CancelReservationOutcome()
    {
    }

    /// <summary>204 No Content — the Reservation is now Cancelada and its Seat is freed.</summary>
    public sealed record Success : CancelReservationOutcome;

    /// <summary>
    /// 409 Conflict — either the Reservation's BoardingRecord is already Abordado, or fewer
    /// than 24h remain before the Trip's departure. Message is the server's own contextual
    /// Spanish text, shown verbatim near the reservation row (it is meaningful on its own,
    /// not a generic failure) — same pattern <see cref="CreateReservationOutcome.Conflict"/>
    /// used for R4's 409.
    /// </summary>
    public sealed record NotCancellable(string Message) : CancelReservationOutcome;

    /// <summary>404 Not Found — the submitted Reservation id doesn't resolve to anything.</summary>
    public sealed record NotFound : CancelReservationOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : CancelReservationOutcome;
}
