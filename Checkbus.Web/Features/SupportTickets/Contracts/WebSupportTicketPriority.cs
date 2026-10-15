namespace Checkbus.Web.Features.SupportTickets.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Domain.Enums.SupportTicketPriority</c>. Checkbus.Web
/// does not reference the API projects (pure BFF), so this enum is duplicated here rather than
/// shared. Member names and underlying numeric values must stay identical to the real
/// <c>SupportTicketPriority</c> enum — no <c>JsonStringEnumConverter</c> is registered anywhere, so
/// the wire value is a plain integer that depends on member order matching exactly.
/// </summary>
public enum WebSupportTicketPriority
{
    Baja,
    Media,
    Alta,
    Urgente
}
