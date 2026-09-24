using Checkbus.Web.Themes;
using MudBlazor;
using MudBlazor.Utilities;

namespace Checkbus.Tests.Themes;

public class CheckbusThemeTests
{
    [Theory]
    [InlineData("Primary", "#005AB5")]
    [InlineData("PrimaryContrastText", "#FFFFFF")]
    [InlineData("Secondary", "#315EA3")]
    [InlineData("SecondaryContrastText", "#FFFFFF")]
    [InlineData("Tertiary", "#984800")]
    [InlineData("TertiaryContrastText", "#FFFFFF")]
    [InlineData("Error", "#BA1A1A")]
    [InlineData("ErrorContrastText", "#FFFFFF")]
    [InlineData("Background", "#F9F9FF")]
    [InlineData("Surface", "#F9F9FF")]
    [InlineData("BackgroundGray", "#F1F3FE")]
    [InlineData("AppbarBackground", "#FFFFFF")]
    [InlineData("DrawerBackground", "#EBEDF8")]
    [InlineData("TextPrimary", "#181C23")]
    [InlineData("AppbarText", "#181C23")]
    [InlineData("TextSecondary", "#414754")]
    [InlineData("DrawerText", "#414754")]
    [InlineData("DrawerIcon", "#414754")]
    [InlineData("ActionDefault", "#717785")]
    [InlineData("LinesInputs", "#717785")]
    [InlineData("LinesDefault", "#C1C6D6")]
    [InlineData("Divider", "#C1C6D6")]
    [InlineData("TableLines", "#C1C6D6")]
    [InlineData("ActionDisabledBackground", "#DDE2F3")]
    [InlineData("TableHover", "#DDE2F3")]
    [InlineData("Skeleton", "#DDE2F3")]
    [InlineData("Dark", "#2D3038")]
    [InlineData("DarkContrastText", "#EEF0FB")]
    public void PaletteLight_MatchesCheckbusJsonLightScheme(string propertyName, string expectedHex)
    {
        var actual = GetPaletteColor(CheckbusTheme.Instance.PaletteLight, propertyName);

        Assert.Equal((MudColor)expectedHex, actual);
    }

    [Theory]
    [InlineData("Primary", "#AAC7FF")]
    [InlineData("PrimaryContrastText", "#002F64")]
    [InlineData("Secondary", "#B9D0FF")]
    [InlineData("SecondaryContrastText", "#002F65")]
    [InlineData("Tertiary", "#FFB68A")]
    [InlineData("TertiaryContrastText", "#522300")]
    [InlineData("Error", "#FFB4AB")]
    [InlineData("ErrorContrastText", "#690005")]
    [InlineData("Background", "#10131A")]
    [InlineData("Surface", "#10131A")]
    [InlineData("BackgroundGray", "#181C23")]
    [InlineData("AppbarBackground", "#1C2027")]
    [InlineData("DrawerBackground", "#1C2027")]
    [InlineData("TextPrimary", "#E0E2EC")]
    [InlineData("AppbarText", "#E0E2EC")]
    [InlineData("TextSecondary", "#C1C6D6")]
    [InlineData("DrawerText", "#C1C6D6")]
    [InlineData("DrawerIcon", "#C1C6D6")]
    [InlineData("ActionDefault", "#8B919F")]
    [InlineData("LinesInputs", "#8B919F")]
    [InlineData("LinesDefault", "#414754")]
    [InlineData("Divider", "#414754")]
    [InlineData("TableLines", "#414754")]
    [InlineData("ActionDisabledBackground", "#414754")]
    [InlineData("TableHover", "#414754")]
    [InlineData("Skeleton", "#414754")]
    [InlineData("Dark", "#E0E2EC")]
    [InlineData("DarkContrastText", "#2D3038")]
    public void PaletteDark_MatchesCheckbusJsonDarkScheme(string propertyName, string expectedHex)
    {
        var actual = GetPaletteColor(CheckbusTheme.Instance.PaletteDark, propertyName);

        Assert.Equal((MudColor)expectedHex, actual);
    }

    private static MudColor GetPaletteColor(Palette palette, string propertyName)
    {
        var property = typeof(Palette).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Palette has no property named '{propertyName}'.");

        return (MudColor)property.GetValue(palette)!;
    }
}
