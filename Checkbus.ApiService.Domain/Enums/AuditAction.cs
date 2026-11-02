namespace Checkbus.ApiService.Domain.Enums
{
    // CU-16 (odd/tasks/auditoria-sistema.md) — the fixed, curated list of sensitive actions that
    // get an AuditLogEntry. Deliberately NOT every mutation in the system (scope agreed with the
    // user 2026-11-01) — matches 1:1 the [Acción] filter options in the use case's main flow.
    public enum AuditAction
    {
        LoginExitoso,
        LoginFallido,
        ViajeCancelado,
        ViajeIniciado,
        ViajeFinalizado,
        DocumentoVehiculoValidado,
        EstadoTicketSoporteCambiado,
        MercadoPagoConectado,
        MercadoPagoDesconectado,
        OrganizacionRegistrada
    }
}
