using System.ComponentModel.DataAnnotations;
using Checkbus.Web.Features.Organizations.Contracts;
using Checkbus.Web.Features.Users.Contracts;

namespace Checkbus.Web.Features.Organizations.Models;

/// <summary>
/// Client-side, two-way-bindable model for the CheckbusSoporte organization-registration form
/// (<c>EditForm</c> + <c>DataAnnotationsValidator</c>). Validation attributes mirror the
/// mechanically-checkable rules in <c>RegisterOrganizationCommandValidator</c> (required, max
/// length, CUIT format, AdminDocumentNumber alphanumeric shape) — the server-only
/// slug-must-contain-a-letter-or-digit and email-generator letter/digit rules are intentionally
/// not duplicated here, same restraint <c>RegisterUserFormModel</c> already applies. This is a
/// plain mutable class, not a record: <c>@bind-Value</c> requires settable properties.
/// </summary>
public sealed class RegisterOrganizationFormModel
{
    [Required(ErrorMessage = "El nombre de la organización es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre de la organización no puede superar los 200 caracteres.")]
    public string OrganizationName { get; set; } = string.Empty;

    // Format only: two digits, hyphen, eight digits, hyphen, one digit (e.g. 20-12345678-9) —
    // same format RegisterOrganizationCommandValidator enforces server-side. Deliberately not a
    // real Argentine check-digit validation; out of scope.
    [Required(ErrorMessage = "El CUIT es obligatorio.")]
    [RegularExpression(@"^\d{2}-\d{8}-\d$", ErrorMessage = "El CUIT debe tener el formato NN-NNNNNNNN-N.")]
    public string CUIT { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del administrador es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre del administrador no puede superar los 100 caracteres.")]
    public string AdminName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del administrador es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido del administrador no puede superar los 100 caracteres.")]
    public string AdminSurname { get; set; } = string.Empty;

    public WebDocumentType AdminDocumentType { get; set; } = WebDocumentType.DNI;

    [Required(ErrorMessage = "El número de documento del administrador es obligatorio.")]
    [StringLength(20, ErrorMessage = "El número de documento no puede superar los 20 caracteres.")]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "El número de documento debe ser alfanumérico.")]
    public string AdminDocumentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Maps this form model to the wire request. Must only be called after validation has
    /// passed, same convention as <c>RegisterUserFormModel.ToRequest()</c>.
    /// </summary>
    public RegisterOrganizationRequest ToRequest() => new()
    {
        OrganizationName = OrganizationName,
        CUIT = CUIT,
        AdminName = AdminName,
        AdminSurname = AdminSurname,
        AdminDocumentType = AdminDocumentType,
        AdminDocumentNumber = AdminDocumentNumber
    };
}
