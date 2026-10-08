using Checkbus.Web.Features.DriverRequirements.Models;
using Checkbus.Web.Features.VehicleDocuments.Models;

namespace Checkbus.Tests.Web;

/// <summary>
/// Unit coverage for <see cref="VehicleDocumentValidity"/>. Unlike
/// <see cref="DriverRequirementValidityTests"/>, this is not a full boundary matrix —
/// <see cref="VehicleDocumentValidity"/> is a pure delegate to <see cref="DriverRequirementValidity"/>
/// (same enum, same thresholds, intentionally not re-implemented to avoid drift), whose boundaries
/// are already exhaustively covered there. These tests exist only to pin the delegation wiring
/// itself — confirming each of the three operations genuinely forwards to and matches
/// <see cref="DriverRequirementValidity"/> — so a future refactor that breaks the forwarding is
/// caught here rather than only at runtime.
/// </summary>
public class VehicleDocumentValidityTests
{
    private static readonly DateOnly Today = new(2026, 10, 2);

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(31)]
    public void Compute_MatchesDriverRequirementValidity_ForEveryBoundaryCase(int? daysFromToday)
    {
        var expirationDate = daysFromToday is null ? (DateOnly?)null : Today.AddDays(daysFromToday.Value);

        var vehicleResult = VehicleDocumentValidity.Compute(expirationDate, Today);
        var driverResult = DriverRequirementValidity.Compute(expirationDate, Today);

        Assert.Equal(driverResult, vehicleResult);
    }

    [Theory]
    [InlineData(ValidityStatus.SinRegistrar)]
    [InlineData(ValidityStatus.Vigente)]
    [InlineData(ValidityStatus.PorVencer)]
    [InlineData(ValidityStatus.Vencido)]
    public void GetLabel_MatchesDriverRequirementValidity_ForEveryState(ValidityStatus state)
    {
        Assert.Equal(DriverRequirementValidity.GetLabel(state), VehicleDocumentValidity.GetLabel(state));
    }

    [Theory]
    [InlineData(ValidityStatus.SinRegistrar)]
    [InlineData(ValidityStatus.Vigente)]
    [InlineData(ValidityStatus.PorVencer)]
    [InlineData(ValidityStatus.Vencido)]
    public void GetColor_MatchesDriverRequirementValidity_ForEveryState(ValidityStatus state)
    {
        Assert.Equal(DriverRequirementValidity.GetColor(state), VehicleDocumentValidity.GetColor(state));
    }
}
