using System.ComponentModel.DataAnnotations;
using Checkbus.Web.Contracts;

namespace Checkbus.Web.Models;

/// <summary>
/// Client-side, two-way-bindable model for the vehicle-registration form (<c>EditForm</c> +
/// <c>DataAnnotationsValidator</c>), mirroring <see cref="RegisterUserFormModel"/>'s pattern.
/// Validation attributes only catch the mechanically-checkable, OwnerType-independent rules
/// (required text fields, Year/Capacity/Mileage ranges mirroring
/// <c>CreateVehicleCommandValidator</c>'s bounds) — the full OwnerType-conditional owner-field
/// validation lives server-side in that same validator and is intentionally not duplicated here;
/// this model only needs to avoid submitting obviously-empty fields for the currently visible
/// sub-form. This is a plain mutable class, not a record: <c>@bind-Value</c> requires settable
/// properties.
/// </summary>
public sealed class VehicleFormModel
{
    private const int MinYear = 1980;

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [StringLength(100, ErrorMessage = "La marca no puede superar los 100 caracteres.")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El modelo no puede superar los 100 caracteres.")]
    public string Model { get; set; } = string.Empty;

    [Range(MinYear, 2100, ErrorMessage = "El año no es válido.")]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    [Required(ErrorMessage = "La patente es obligatoria.")]
    [StringLength(20, ErrorMessage = "La patente no puede superar los 20 caracteres.")]
    public string Patent { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "La capacidad no puede ser negativa.")]
    public int Capacity { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El kilometraje no puede ser negativo.")]
    public int Mileage { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio.")]
    public WebVehicleStatus Status { get; set; } = WebVehicleStatus.Activo;

    [Required(ErrorMessage = "El tipo de titularidad es obligatorio.")]
    public WebVehicleOwnerType OwnerType { get; set; } = WebVehicleOwnerType.Organizacion;

    /// <summary>Relevant only when <see cref="OwnerType"/> is <see cref="WebVehicleOwnerType.Chofer"/>.</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>Relevant only when <see cref="OwnerType"/> is <see cref="WebVehicleOwnerType.Otro"/>.</summary>
    public string? OwnerName { get; set; }

    /// <summary>Relevant only when <see cref="OwnerType"/> is <see cref="WebVehicleOwnerType.Otro"/>.</summary>
    public string? OwnerDocumentNumber { get; set; }

    /// <summary>
    /// Maps this form model to the wire request, nulling out whichever owner sub-fields do not
    /// apply to the currently selected <see cref="OwnerType"/> — the server validator rejects a
    /// request carrying owner fields that don't match its <c>OwnerType</c>, so this must never
    /// forward stale values left over from a sub-form the admin switched away from.
    /// </summary>
    public CreateVehicleRequest ToRequest() => new()
    {
        Brand = Brand,
        Model = Model,
        Year = Year,
        Patent = Patent,
        Capacity = Capacity,
        Mileage = Mileage,
        Status = Status,
        OwnerType = OwnerType,
        OwnerUserId = OwnerType == WebVehicleOwnerType.Chofer ? OwnerUserId : null,
        OwnerName = OwnerType == WebVehicleOwnerType.Otro ? OwnerName : null,
        OwnerDocumentNumber = OwnerType == WebVehicleOwnerType.Otro ? OwnerDocumentNumber : null
    };
}
