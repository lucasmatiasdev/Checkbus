using Checkbus.Web.Features.DriverRequirements.Models;

namespace Checkbus.Tests.Web;

/// <summary>
/// Unit coverage for the plain-code validity calculation in <see cref="DriverRequirementValidity"/>.
/// This is the one piece of T4's date-threshold logic that gets a real test — the Razor page
/// itself only gets the reflection-based authorization test, per this repo's no-bUnit convention.
/// </summary>
public class DriverRequirementValidityTests
{
    private static readonly DateOnly Today = new(2026, 10, 2);

    [Fact]
    public void Compute_NullExpirationDate_ReturnsSinRegistrar()
    {
        var result = DriverRequirementValidity.Compute(null, Today);

        Assert.Equal(ValidityStatus.SinRegistrar, result);
    }

    [Fact]
    public void Compute_DateBeforeToday_ReturnsVencido()
    {
        var result = DriverRequirementValidity.Compute(Today.AddDays(-1), Today);

        Assert.Equal(ValidityStatus.Vencido, result);
    }

    [Fact]
    public void Compute_DateExactlyToday_DoesNotReturnVencido()
    {
        var result = DriverRequirementValidity.Compute(Today, Today);

        Assert.NotEqual(ValidityStatus.Vencido, result);
    }

    [Theory]
    [InlineData(29)]
    [InlineData(30)]
    public void Compute_WithinThresholdBoundary_ReturnsPorVencer(int daysAhead)
    {
        var result = DriverRequirementValidity.Compute(Today.AddDays(daysAhead), Today);

        Assert.Equal(ValidityStatus.PorVencer, result);
    }

    [Fact]
    public void Compute_JustPastThreshold_ReturnsVigente()
    {
        var result = DriverRequirementValidity.Compute(Today.AddDays(31), Today);

        Assert.Equal(ValidityStatus.Vigente, result);
    }

    [Fact]
    public void Compute_CustomThreshold_MovesBoundary()
    {
        var withinCustomThreshold = DriverRequirementValidity.Compute(Today.AddDays(10), Today, thresholdDays: 10);
        var beyondCustomThreshold = DriverRequirementValidity.Compute(Today.AddDays(11), Today, thresholdDays: 10);

        Assert.Equal(ValidityStatus.PorVencer, withinCustomThreshold);
        Assert.Equal(ValidityStatus.Vigente, beyondCustomThreshold);
    }
}
