using MudBlazor;
using Checkbus.Web.Features.Users.Models;

namespace Checkbus.Web.Features.DriverRequirements.Models;

/// <summary>
/// Pure, plain-code validity calculation shared by the Chofer self-service page
/// (<c>MyDocuments.razor</c>) and, later, the Administrador review page (T5). Pushed out of Razor
/// markup so the date-threshold logic is unit-testable without bUnit (this repo has none), same
/// pattern as <see cref="UserListFilter"/>. <paramref name="today"/> is always taken as a
/// parameter rather than read from <see cref="DateTime.Now"/> internally, keeping the calculation
/// deterministic and testable.
/// </summary>
public static class DriverRequirementValidity
{
    /// <summary>
    /// Computes the validity state for a requirement's <paramref name="expirationDate"/> as of
    /// <paramref name="today"/>. A <see langword="null"/> expiration date (never uploaded yet)
    /// is <see cref="ValidityStatus.SinRegistrar"/>. A date strictly before <paramref name="today"/>
    /// is <see cref="ValidityStatus.Vencido"/> — today itself still counts as valid. A date within
    /// <paramref name="thresholdDays"/> days from today (inclusive) is
    /// <see cref="ValidityStatus.PorVencer"/>; anything further out is
    /// <see cref="ValidityStatus.Vigente"/>.
    /// </summary>
    public static ValidityStatus Compute(DateOnly? expirationDate, DateOnly today, int thresholdDays = 30)
    {
        if (expirationDate is null)
            return ValidityStatus.SinRegistrar;

        if (expirationDate.Value < today)
            return ValidityStatus.Vencido;

        if (expirationDate.Value <= today.AddDays(thresholdDays))
            return ValidityStatus.PorVencer;

        return ValidityStatus.Vigente;
    }

    /// <summary>Spanish display label for a <see cref="ValidityStatus"/>, used by the validity badge.</summary>
    public static string GetLabel(ValidityStatus state) => state switch
    {
        ValidityStatus.SinRegistrar => "Sin registrar",
        ValidityStatus.Vigente => "Vigente",
        ValidityStatus.PorVencer => "Por vencer",
        ValidityStatus.Vencido => "Vencido",
        _ => state.ToString()
    };

    /// <summary>MudBlazor badge color for a <see cref="ValidityStatus"/>.</summary>
    public static Color GetColor(ValidityStatus state) => state switch
    {
        ValidityStatus.Vigente => Color.Success,
        ValidityStatus.PorVencer => Color.Warning,
        ValidityStatus.Vencido => Color.Error,
        _ => Color.Default
    };
}
