using MudBlazor;

namespace Checkbus.Web.Models;

/// <summary>
/// Pure, plain-code vigencia calculation for vehicle documents, used by the admin
/// upload-and-validate page (<c>VehicleDocuments.razor</c>). A vehicle document's
/// "vigente/por vencer/vencido" classification is the exact same concept as a driver
/// requirement's — same 30-day threshold, same "today itself is still valid" boundary — so this
/// class reuses the existing <see cref="VigenciaState"/> enum rather than forking a second,
/// functionally-identical one, and delegates its three operations straight to
/// <see cref="DriverRequirementVigencia"/> rather than re-implementing (and risking drifting) the
/// same date-threshold logic. It is kept as its own named class — not just a call-site alias to
/// <see cref="DriverRequirementVigencia"/> — so the vehicle-documents and driver-requirements
/// pages can diverge independently later (e.g. a different threshold for one document family)
/// without one page's change silently affecting the other's.
/// </summary>
public static class VehicleDocumentVigencia
{
    /// <summary>See <see cref="DriverRequirementVigencia.Compute"/> — identical semantics.</summary>
    public static VigenciaState Compute(DateOnly? expirationDate, DateOnly today, int thresholdDays = 30) =>
        DriverRequirementVigencia.Compute(expirationDate, today, thresholdDays);

    /// <summary>See <see cref="DriverRequirementVigencia.GetLabel"/> — identical semantics.</summary>
    public static string GetLabel(VigenciaState state) => DriverRequirementVigencia.GetLabel(state);

    /// <summary>See <see cref="DriverRequirementVigencia.GetColor"/> — identical semantics.</summary>
    public static Color GetColor(VigenciaState state) => DriverRequirementVigencia.GetColor(state);
}
