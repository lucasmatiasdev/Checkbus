namespace Checkbus.Web.Features.DriverRequirements.Models;

/// <summary>
/// The validity ("vigencia") state of a driver requirement's expiration date, computed at
/// render/query time rather than stored — see the feature doc's "vigencia is never a stored
/// column" constraint.
/// </summary>
public enum ValidityStatus
{
    SinRegistrar,
    Vigente,
    PorVencer,
    Vencido
}
