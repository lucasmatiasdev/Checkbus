namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Domain.Authorization.Role</c>. Checkbus.Web does
/// not reference the API projects (pure BFF), so this enum is duplicated here rather than
/// shared. Member names and underlying numeric values must stay identical to the real
/// <c>Role</c> enum — <c>Checkbus.Tests</c> asserts this parity via reflection.
/// </summary>
public enum WebRole
{
    Administrador,
    Chofer,
    Planificador,
    Mecanico
}
