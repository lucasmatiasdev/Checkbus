namespace Checkbus.ApiService.Domain.Enums
{
    // Lifecycle of one BoardingRecord row (odd/tasks/reservas-publicas.md Fase 3 / A1).
    // Pendiente is the only possible value at row-creation time (EnsureForTripAsync inserts
    // every missing row as Pendiente, mirroring Reservation.Status's "always starts in its
    // initial state" convention). Abordado is set once by ValidateAsync; Ausente is set in
    // bulk by CloseBoardingAsync for every row still Pendiente when the Chofer closes
    // boarding for the Trip. Neither terminal state is ever reversed in this phase.
    public enum BoardingStatus
    {
        Pendiente,
        Abordado,
        Ausente
    }
}
