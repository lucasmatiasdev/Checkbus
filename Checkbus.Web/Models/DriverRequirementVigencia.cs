using MudBlazor;

namespace Checkbus.Web.Models;

/// <summary>
/// The vigencia ("validity") state of a driver requirement's expiration date, computed at
/// render/query time rather than stored — see the feature doc's "vigencia is never a stored
/// column" constraint.
/// </summary>
public enum VigenciaState
{
    SinRegistrar,
    Vigente,
    PorVencer,
    Vencido
}

/// <summary>
/// Pure, plain-code vigencia calculation shared by the Chofer self-service page
/// (<c>MyDocuments.razor</c>) and, later, the Administrador review page (T5). Pushed out of Razor
/// markup so the date-threshold logic is unit-testable without bUnit (this repo has none), same
/// pattern as <see cref="UserListFilter"/>. <paramref name="today"/> is always taken as a
/// parameter rather than read from <see cref="DateTime.Now"/> internally, keeping the calculation
/// deterministic and testable.
/// </summary>
public static class DriverRequirementVigencia
{
    /// <summary>
    /// Computes the vigencia state for a requirement's <paramref name="expirationDate"/> as of
    /// <paramref name="today"/>. A <see langword="null"/> expiration date (never uploaded yet)
    /// is <see cref="VigenciaState.SinRegistrar"/>. A date strictly before <paramref name="today"/>
    /// is <see cref="VigenciaState.Vencido"/> — today itself still counts as valid. A date within
    /// <paramref name="thresholdDays"/> days from today (inclusive) is
    /// <see cref="VigenciaState.PorVencer"/>; anything further out is
    /// <see cref="VigenciaState.Vigente"/>.
    /// </summary>
    public static VigenciaState Compute(DateOnly? expirationDate, DateOnly today, int thresholdDays = 30)
    {
        if (expirationDate is null)
            return VigenciaState.SinRegistrar;

        if (expirationDate.Value < today)
            return VigenciaState.Vencido;

        if (expirationDate.Value <= today.AddDays(thresholdDays))
            return VigenciaState.PorVencer;

        return VigenciaState.Vigente;
    }

    /// <summary>Spanish display label for a <see cref="VigenciaState"/>, used by the vigencia badge.</summary>
    public static string GetLabel(VigenciaState state) => state switch
    {
        VigenciaState.SinRegistrar => "Sin registrar",
        VigenciaState.Vigente => "Vigente",
        VigenciaState.PorVencer => "Por vencer",
        VigenciaState.Vencido => "Vencido",
        _ => state.ToString()
    };

    /// <summary>MudBlazor badge color for a <see cref="VigenciaState"/>.</summary>
    public static Color GetColor(VigenciaState state) => state switch
    {
        VigenciaState.Vigente => Color.Success,
        VigenciaState.PorVencer => Color.Warning,
        VigenciaState.Vencido => Color.Error,
        _ => Color.Default
    };
}
