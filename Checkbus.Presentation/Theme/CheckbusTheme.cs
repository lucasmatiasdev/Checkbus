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
    /// #6F5090). MudBlazor's <see cref="Palette"/> has no Material 3 "container"/"on-X" roles, so
    /// M3 roles are mapped onto MudBlazor's flatter Material 2-style model: each M3 "onX" role
    /// (the color for content drawn ON TOP of X) becomes MudBlazor's "XContrastText"; "surface"
    /// roles become Background/Surface/DrawerBackground; "outlineVariant" (a subtle border tone)
    /// becomes LinesDefault.
    /// </summary>
    public static class CheckbusTheme
    {
        public static readonly MudTheme Default = new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#563876",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#675974",
                SecondaryContrastText = "#FFFFFF",
                Tertiary = "#732F4A",
                TertiaryContrastText = "#FFFFFF",
                Error = "#BA1A1A",
                ErrorContrastText = "#FFFFFF",
                Background = "#FFF7FE",
                BackgroundGray = "#E9DFEC",
                Surface = "#FFF7FE",
                DrawerBackground = "#FFF7FE",
                DrawerText = "#1D1B1F",
                DrawerIcon = "#4B454F",
                AppbarBackground = "#563876",
                AppbarText = "#FFFFFF",
                TextPrimary = "#1D1B1F",
                TextSecondary = "#4B454F",
                LinesDefault = "#CDC3D0",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#DCB8FF",
                PrimaryContrastText = "#3F215F",
                Secondary = "#D2C0E0",
                SecondaryContrastText = "#372B44",
                Tertiary = "#FFB0CA",
                TertiaryContrastText = "#571934",
                Error = "#FFB4AB",
                ErrorContrastText = "#690005",
                Background = "#151217",
                BackgroundGray = "#4B454F",
                Surface = "#151217",
                DrawerBackground = "#151217",
                DrawerText = "#E7E0E7",
                DrawerIcon = "#CDC3D0",
                AppbarBackground = "#DCB8FF",
                AppbarText = "#3F215F",
                TextPrimary = "#E7E0E7",
                TextSecondary = "#CDC3D0",
                LinesDefault = "#4B454F",
            },
        };
    }
}
