using Checkbus.Web.Models;

namespace Checkbus.Tests.Web;

/// <summary>
/// Unit coverage for the plain-code vigencia calculation in <see cref="DriverRequirementVigencia"/>.
/// This is the one piece of T4's date-threshold logic that gets a real test — the Razor page
/// itself only gets the reflection-based authorization test, per this repo's no-bUnit convention.
/// </summary>
public class DriverRequirementVigenciaTests
{
    private static readonly DateOnly Today = new(2026, 10, 2);

    [Fact]
    public void Compute_NullExpirationDate_ReturnsSinRegistrar()
    {
        var result = DriverRequirementVigencia.Compute(null, Today);

        Assert.Equal(VigenciaState.SinRegistrar, result);
    }

    [Fact]
    public void Compute_DateBeforeToday_ReturnsVencido()
    {
        var result = DriverRequirementVigencia.Compute(Today.AddDays(-1), Today);

        Assert.Equal(VigenciaState.Vencido, result);
    }

    [Fact]
    public void Compute_DateExactlyToday_DoesNotReturnVencido()
    {
        var result = DriverRequirementVigencia.Compute(Today, Today);

        Assert.NotEqual(VigenciaState.Vencido, result);
    }

    [Theory]
    [InlineData(29)]
    [InlineData(30)]
    public void Compute_WithinThresholdBoundary_ReturnsPorVencer(int daysAhead)
    {
        var result = DriverRequirementVigencia.Compute(Today.AddDays(daysAhead), Today);

        Assert.Equal(VigenciaState.PorVencer, result);
    }

    [Fact]
    public void Compute_JustPastThreshold_ReturnsVigente()
    {
        var result = DriverRequirementVigencia.Compute(Today.AddDays(31), Today);

        Assert.Equal(VigenciaState.Vigente, result);
    }

    [Fact]
    public void Compute_CustomThreshold_MovesBoundary()
    {
        var withinCustomThreshold = DriverRequirementVigencia.Compute(Today.AddDays(10), Today, thresholdDays: 10);
        var beyondCustomThreshold = DriverRequirementVigencia.Compute(Today.AddDays(11), Today, thresholdDays: 10);

        Assert.Equal(VigenciaState.PorVencer, withinCustomThreshold);
        Assert.Equal(VigenciaState.Vigente, beyondCustomThreshold);
    }
}
