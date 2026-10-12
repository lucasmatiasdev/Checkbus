using Checkbus.Web.Features.PublicReservations.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/public/trips/{id}/reservations</c> call
/// (odd/tasks/reservas-publicas.md R4). Mirrors <see cref="PublicTripSeatMapOutcome"/>'s shape,
/// plus a dedicated <see cref="Conflict"/> case for the all-or-nothing double-booking guard's
/// 409 — distinct from a generic <see cref="TransportError"/> so the page can react
/// specifically (clear selection, refetch the seat map) instead of showing a generic failure.
/// </summary>
public abstract record CreateReservationOutcome
{
    private CreateReservationOutcome()
    {
    }

    /// <summary>201 Created — the whole batch was reserved successfully.</summary>
    public sealed record Success(CreateReservationResponse Result) : CreateReservationOutcome;

    /// <summary>
    /// 400 Bad Request — FluentValidation structural failures keyed by property name.
    /// </summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : CreateReservationOutcome;

    /// <summary>
    /// 409 Conflict — one or more selected seats are no longer available (taken by someone
    /// else between page-load and submit, or a SeatId that doesn't belong to this trip's
    /// vehicle). Nothing from the batch was persisted.
    /// </summary>
    public sealed record Conflict(string Message) : CreateReservationOutcome;

    /// <summary>
    /// 404 Not Found — either no Trip exists with the given id, or it exists but is not
    /// Publicado. Both cases are indistinguishable by design (IDOR-safe).
    /// </summary>
    public sealed record NotFound : CreateReservationOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : CreateReservationOutcome;
}
