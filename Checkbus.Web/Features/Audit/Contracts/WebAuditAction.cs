namespace Checkbus.Web.Features.Audit.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Domain.Enums.AuditAction</c> (CU-16,
/// <c>odd/tasks/auditoria-sistema.md</c>). Checkbus.Web does not reference the API projects (pure
/// BFF), so this enum is duplicated here rather than shared. Member names and underlying numeric
/// values must stay identical to the real <c>AuditAction</c> enum — no
/// <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/> is registered anywhere in
/// this solution (same as every other enum mirror here), so <c>AuditLogDto.Action</c> crosses the
/// wire as a plain integer, not as the enum's name. Unlike <c>WebRole</c>/<c>WebTripStatus</c>,
/// there is no <c>Checkbus.Tests</c> reflection parity test for this mirror yet — adding one is
/// outside this task's allowed edit surface (<c>Checkbus.Tests</c> is not touched by T5); any
/// future addition/removal/renumbering on the real <c>AuditAction</c> must be mirrored here by
/// hand.
/// </summary>
public enum WebAuditAction
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
