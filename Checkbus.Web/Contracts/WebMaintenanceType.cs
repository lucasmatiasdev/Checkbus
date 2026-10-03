namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Domain.Enums.MaintenanceType</c>. Checkbus.Web does
/// not reference the API projects (pure BFF), so this enum is duplicated here rather than shared.
/// Member names and underlying numeric values must stay identical to the real
/// <c>MaintenanceType</c> enum — no <c>JsonStringEnumConverter</c> is registered anywhere, so the
/// wire value is a plain integer that depends on member order matching exactly.
/// </summary>
public enum WebMaintenanceType
{
    Preventivo,
    Reactivo
}
