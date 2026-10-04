namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Domain.Enums.StopTipo</c>. Checkbus.Web does not
/// reference the API projects (pure BFF), so this enum is duplicated here rather than shared.
/// Member names and underlying numeric values must stay identical to the real <c>StopTipo</c>
/// enum — <c>Checkbus.Tests</c> asserts this parity via reflection.
/// </summary>
public enum WebStopTipo
{
    Origen,
    Intermedia,
    Destino
}
