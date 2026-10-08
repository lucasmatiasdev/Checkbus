namespace Checkbus.ApiService.Domain.Enums
{
    // Only Confirmada exists in Fase 1 of reservas-publicas: a Reservation is confirmed
    // immediately on creation, there is no payment/pending step. Cancelada arrives with
    // Fase 4 (cancellation) — deliberately not added yet.
    public enum ReservationStatus
    {
        Confirmada
    }
}
