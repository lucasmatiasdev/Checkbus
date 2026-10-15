namespace Checkbus.ApiService.Domain.Enums
{
    // Confirmada was the only value in Fase 1-3 of reservas-publicas: a Reservation is
    // confirmed immediately on creation, there is no payment/pending step. Cancelada arrives
    // with Fase 4 (cancellation) — a one-way terminal transition, there is no "un-cancel" in
    // this phase. See CheckbusDbContext.OnModelCreating for the filtered unique index that
    // lets a Cancelada row's Seat be re-booked by someone else.
    // PendientePago arrives with pagos-mercadopago Fase A: a Reservation is now created in
    // this state BEFORE redirecting to Mercado Pago checkout, locking the seat while payment
    // is in flight. The filtered unique index on (TripId, SeatId) already excludes only
    // Cancelada, so a PendientePago row blocks the seat for anyone else with no index change
    // needed. It flips to Confirmada on webhook approval, or back to Cancelada on
    // rejection/lazy expiration (see odd/tasks/pagos-mercadopago.md Fase D/E).
    public enum ReservationStatus
    {
        Confirmada,
        Cancelada,
        PendientePago
    }
}
