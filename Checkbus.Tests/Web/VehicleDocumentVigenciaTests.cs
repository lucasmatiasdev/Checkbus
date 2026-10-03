using Checkbus.Web.Models;

namespace Checkbus.Tests.Web;

/// <summary>
/// Unit coverage for <see cref="VehicleDocumentVigencia"/>. Unlike
/// <see cref="DriverRequirementVigenciaTests"/>, this is not a full boundary matrix —
/// <see cref="VehicleDocumentVigencia"/> is a pure delegate to <see cref="DriverRequirementVigencia"/>
/// (same enum, same thresholds, intentionally not re-implemented to avoid drift), whose boundaries
/// are already exhaustively covered there. These tests exist only to pin the delegation wiring
/// itself — confirming each of the three operations genuinely forwards to and matches
/// <see cref="DriverRequirementVigencia"/> — so a future refactor that breaks the forwarding is
/// caught here rather than only at runtime.
/// </summary>
public class VehicleDocumentVigenciaTests
{
    private static readonly DateOnly Today = new(2026, 10, 2);

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(31)]
    public void Compute_MatchesDriverRequirementVigencia_ForEveryBoundaryCase(int? daysFromToday)
    {
        var expirationDate = daysFromToday is null ? (DateOnly?)null : Today.AddDays(daysFromToday.Value);

        var vehicleResult = VehicleDocumentVigencia.Compute(expirationDate, Today);
        var driverResult = DriverRequirementVigencia.Compute(expirationDate, Today);

        Assert.Equal(driverResult, vehicleResult);
    }

    [Theory]
    [InlineData(VigenciaState.SinRegistrar)]
    [InlineData(VigenciaState.Vigente)]
    [InlineData(VigenciaState.PorVencer)]
    [InlineData(VigenciaState.Vencido)]
    public void GetLabel_MatchesDriverRequirementVigencia_ForEveryState(VigenciaState state)
    {
        Assert.Equal(DriverRequirementVigencia.GetLabel(state), VehicleDocumentVigencia.GetLabel(state));
    }

    [Theory]
    [InlineData(VigenciaState.SinRegistrar)]
    [InlineData(VigenciaState.Vigente)]
    [InlineData(VigenciaState.PorVencer)]
    [InlineData(VigenciaState.Vencido)]
    public void GetColor_MatchesDriverRequirementVigencia_ForEveryState(VigenciaState state)
    {
        Assert.Equal(DriverRequirementVigencia.GetColor(state), VehicleDocumentVigencia.GetColor(state));
    }
}
