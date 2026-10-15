namespace Checkbus.Web.Features.SupportTickets.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Domain.Enums.SupportTicketStatus</c>. Checkbus.Web
/// does not reference the API projects (pure BFF), so this enum is duplicated here rather than
/// shared. Member names and underlying numeric values must stay identical to the real
/// <c>SupportTicketStatus</c> enum — no <c>JsonStringEnumConverter</c> is registered anywhere, so
/// the wire value is a plain integer that depends on member order matching exactly. This status is
/// read-only on the web side: nothing in Checkbus.Web ever sends this value back to the API, it is
/// only ever displayed.
/// </summary>
public enum WebSupportTicketStatus
{
    Abierto,
    EnRevision,
    EnProgreso,
    EsperandoCliente,
    Resuelto,
    Reabierto,
    Cerrado
}
