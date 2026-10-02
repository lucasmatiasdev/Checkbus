namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Domain.Enums.DocumentType</c>. Checkbus.Web does
/// not reference the API projects (pure BFF), so this enum is duplicated here rather than
/// shared. Member names and underlying numeric values must stay identical to the real
/// <c>DocumentType</c> enum — <c>Checkbus.Tests</c> asserts this parity via reflection.
/// </summary>
public enum WebDocumentType
{
    DNI = 0,
    Pasaporte = 1
}
