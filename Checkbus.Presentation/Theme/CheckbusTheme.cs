using MudBlazor;

namespace Checkbus.Presentation.Theme
{
    /// <summary>
    /// Single MudBlazor theme shared across both render-mode zones (design D-F1/D-F3): the
    /// static <c>AuthLayout</c> hosting Login and the static <c>MainLayout</c> hosting the
    /// interactive shell. Defined once here so both layouts stay visually identical without
    /// duplicating palette values.
    /// </summary>
    public static class CheckbusTheme
    {
        public static readonly MudTheme Default = new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#1E88E5",
                Secondary = "#26A69A",
                AppbarBackground = "#1E88E5",
                Background = "#F5F7FA",
            },
        };
    }
}
