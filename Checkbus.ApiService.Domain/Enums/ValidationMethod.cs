namespace Checkbus.ApiService.Domain.Enums
{
    // How a BoardingRecord was moved to Abordado (odd/tasks/reservas-publicas.md Fase 3 / A1).
    // Deliberately named after what actually happens today, not future camera-based QR
    // scanning (explicitly out of scope this phase — see the Fase 3 analysis note in
    // odd/tasks/reservas-publicas.md): Codigo is the Chofer typing/pasting the
    // Reservation.Id itself, Busqueda is the Chofer picking the row out of the name/DNI
    // search list. Renaming later if camera scanning lands is a one-enum-value addition,
    // not a rename, since the stored strings (HasConversion<string>()) must stay meaningful
    // either way.
    public enum ValidationMethod
    {
        Codigo,
        Busqueda
    }
}
