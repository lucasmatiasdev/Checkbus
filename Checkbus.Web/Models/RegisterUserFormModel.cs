using System.ComponentModel.DataAnnotations;
using Checkbus.Web.Contracts;

namespace Checkbus.Web.Models;

/// <summary>
/// Client-side, two-way-bindable model for the user-registration form (<c>EditForm</c> +
/// <c>DataAnnotationsValidator</c>). Validation attributes mirror the mechanically-checkable
/// rules in <c>RegisterUserCommandValidator</c> (required, max length, DocumentNumber alphanumeric
/// shape) — the server-only email-generator letter/digit rule is intentionally not duplicated here.
/// This is a plain mutable class, not a record: <c>@bind-Value</c> requires settable properties.
/// </summary>
public sealed class RegisterUserFormModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string Surname { get; set; } = string.Empty;

    public WebDocumentType DocumentType { get; set; } = WebDocumentType.DNI;

    [Required(ErrorMessage = "El número de documento es obligatorio.")]
    [StringLength(20, ErrorMessage = "El número de documento no puede superar los 20 caracteres.")]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "El número de documento debe ser alfanumérico.")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public WebRole? Role { get; set; }

    /// <summary>
    /// Roles assignable from this form. Excludes <see cref="WebRole.Administrador"/> on purpose —
    /// this registration screen must never be usable to create another Administrador.
    /// </summary>
    public static IReadOnlyList<WebRole> AssignableRoles { get; } =
        Enum.GetValues<WebRole>().Where(role => role != WebRole.Administrador).ToArray();

    /// <summary>
    /// Maps this form model to the wire request. Must only be called after validation has
    /// passed — <see cref="Role"/> is asserted non-null here rather than re-validated.
    /// </summary>
    public RegisterUserRequest ToRequest() => new()
    {
        Name = Name,
        Surname = Surname,
        DocumentType = DocumentType,
        DocumentNumber = DocumentNumber,
        Role = Role!.Value
    };
}
