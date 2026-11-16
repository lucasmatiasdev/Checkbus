namespace Checkbus.ApiService.Domain.Enums
{
    // How a BoardingRecord was moved to Abordado (odd/tasks/reservas-publicas.md Fase 3 / A1,
    // extended by odd/tasks/validacion-embarque-qr.md). Codigo is the Chofer typing/pasting the
    // Reservation.Id itself, Busqueda is the Chofer picking the row out of the name/DNI search
    // list, Qr is the Chofer scanning the passenger's QR ticket (same Reservation.Id, read via
    // the browser's camera instead of typed). Adding Qr was a pure enum-value addition, not a
    // rename, since the stored strings (HasConversion<string>()) must stay meaningful either way.
    public enum ValidationMethod
    {
        Codigo,
        Busqueda,
        Qr
    }
}
