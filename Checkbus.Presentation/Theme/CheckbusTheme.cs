using MudBlazor;

namespace Checkbus.Presentation.Theme
{
    /// <summary>
    /// Single MudBlazor theme shared across both render-mode zones (design D-F1/D-F3): the
    /// static <c>AuthLayout</c> hosting Login and the static <c>MainLayout</c> hosting the
    /// interactive shell. Defined once here so both layouts stay visually identical without
    /// duplicating palette values.
    ///
    /// Colors sourced from <c>material-theme.json</c> (Material Theme Builder export, seed
    /// #1565C0 — a transit/institutional blue chosen for the fleet-management domain). MudBlazor's
    /// <see cref="Palette"/> has no Material 3 "container"/"on-X" roles, so M3 roles are mapped
    /// onto MudBlazor's flatter Material 2-style model: each M3 "onX" role (the color for content
    /// drawn ON TOP of X) becomes MudBlazor's "XContrastText"; "surface" roles become
    /// Background/Surface/DrawerBackground; "outlineVariant" (a subtle border tone) becomes
    /// LinesDefault.
    /// </summary>
    public static class CheckbusTheme
    {
        public static readonly MudTheme Default = new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#405F90",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#555F71",
                SecondaryContrastText = "#FFFFFF",
                Tertiary = "#3C6090",
                TertiaryContrastText = "#FFFFFF",
                Error = "#BA1A1A",
                ErrorContrastText = "#FFFFFF",
                Background = "#F9F9FF",
                BackgroundGray = "#E0E2EC",
                Surface = "#F9F9FF",
                DrawerBackground = "#F9F9FF",
                DrawerText = "#191C20",
                DrawerIcon = "#44474E",
                AppbarBackground = "#405F90",
                AppbarText = "#FFFFFF",
                TextPrimary = "#191C20",
                TextSecondary = "#44474E",
                LinesDefault = "#C4C6CF",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#A9C7FF",
                PrimaryContrastText = "#08305F",
                Secondary = "#BDC7DC",
                SecondaryContrastText = "#283141",
                Tertiary = "#A5C8FF",
                TertiaryContrastText = "#00315E",
                Error = "#FFB4AB",
                ErrorContrastText = "#690005",
                Background = "#111318",
                BackgroundGray = "#44474E",
                Surface = "#111318",
                DrawerBackground = "#111318",
                DrawerText = "#E2E2E9",
                DrawerIcon = "#C4C6CF",
                AppbarBackground = "#A9C7FF",
                AppbarText = "#08305F",
                TextPrimary = "#E2E2E9",
                TextSecondary = "#C4C6CF",
                LinesDefault = "#44474E",
            },
        };
    }
}
