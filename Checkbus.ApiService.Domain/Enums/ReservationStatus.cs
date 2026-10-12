namespace Checkbus.ApiService.Domain.Enums
{
    // Confirmada was the only value in Fase 1-3 of reservas-publicas: a Reservation is
    // confirmed immediately on creation, there is no payment/pending step. Cancelada arrives
    // with Fase 4 (cancellation) — a one-way terminal transition, there is no "un-cancel" in
    // this phase. See CheckbusDbContext.OnModelCreating for the filtered unique index that
    // lets a Cancelada row's Seat be re-booked by someone else.
    public enum ReservationStatus
    {
        Confirmada,
        Cancelada
    }
}
