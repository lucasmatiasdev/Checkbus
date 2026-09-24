using MudBlazor;

namespace Checkbus.Web.Themes;

/// <summary>
/// Single shared Checkbus <see cref="MudTheme"/> instance, derived from the Material Design 3
/// export at <c>Checkbus.Web/Themes/checkbus.json</c>. Referenced by both layout
/// <c>MudThemeProvider</c>s so the app never falls back to MudBlazor's stock default palette.
/// </summary>
public static class CheckbusTheme
{
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#005AB5",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#315EA3",
            SecondaryContrastText = "#FFFFFF",
            Tertiary = "#984800",
            TertiaryContrastText = "#FFFFFF",
            Error = "#BA1A1A",
            ErrorContrastText = "#FFFFFF",
            Background = "#F9F9FF",
            Surface = "#F9F9FF",
            BackgroundGray = "#F1F3FE",
            AppbarBackground = "#FFFFFF",
            DrawerBackground = "#EBEDF8",
            TextPrimary = "#181C23",
            AppbarText = "#181C23",
            TextSecondary = "#414754",
            DrawerText = "#414754",
            DrawerIcon = "#414754",
            ActionDefault = "#717785",
            LinesInputs = "#717785",
            LinesDefault = "#C1C6D6",
            Divider = "#C1C6D6",
            TableLines = "#C1C6D6",
            ActionDisabledBackground = "#DDE2F3",
            TableHover = "#DDE2F3",
            Skeleton = "#DDE2F3",
            Dark = "#2D3038",
            DarkContrastText = "#EEF0FB",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#AAC7FF",
            PrimaryContrastText = "#002F64",
            Secondary = "#B9D0FF",
            SecondaryContrastText = "#002F65",
            Tertiary = "#FFB68A",
            TertiaryContrastText = "#522300",
            Error = "#FFB4AB",
            ErrorContrastText = "#690005",
            Background = "#10131A",
            Surface = "#10131A",
            BackgroundGray = "#181C23",
            AppbarBackground = "#1C2027",
            DrawerBackground = "#1C2027",
            TextPrimary = "#E0E2EC",
            AppbarText = "#E0E2EC",
            TextSecondary = "#C1C6D6",
            DrawerText = "#C1C6D6",
            DrawerIcon = "#C1C6D6",
            ActionDefault = "#8B919F",
            LinesInputs = "#8B919F",
            LinesDefault = "#414754",
            Divider = "#414754",
            TableLines = "#414754",
            ActionDisabledBackground = "#414754",
            TableHover = "#414754",
            Skeleton = "#414754",
            Dark = "#E0E2EC",
            DarkContrastText = "#2D3038",
        },
    };
}
