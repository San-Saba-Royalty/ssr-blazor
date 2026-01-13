using MudBlazor;

namespace SSRBlazor.Components.Themes;

public static class SSRIntegrationTheme
{
    private static MudTheme? _defaultTheme;

    public static MudTheme DefaultTheme => _defaultTheme ??= GetTheme();

    private static MudTheme GetTheme()
    {
        var theme = new MudTheme();

        // Palette Light
        theme.PaletteLight = new PaletteLight()
        {
            Primary = "#005a5d",
            Secondary = "#0b394f",
            Tertiary = "#2D8B8B",
            Info = "#0b394f",
            Success = "#005a5d",
            Warning = "#FFC107",
            Error = "#F44336",
            AppbarBackground = "#005a5d",
            AppbarText = "#FFFFFF",
            Background = "#EEEEEE",
            Surface = "#FFFFFF",
            TextPrimary = "#5c5b59", 
            TextSecondary = "#005a5d",
            ActionDefault = "#005a5d", 
            DrawerBackground = "#FFFFFF",
            DrawerText = "#5c5b59",
            LinesDefault = "rgba(0,0,0,0)",
            TableLines = "rgba(0,0,0,0)",
            Divider = "rgba(0,0,0,0)",
            OverlayLight = "rgba(0, 90, 93, 0.6)"
        };

        // Palette Dark
        theme.PaletteDark = new PaletteDark()
        {
            Primary = "#2D8B8B",
            Secondary = "#005a5d",
            Tertiary = "#0b394f",
            Info = "#2D8B8B",
            Success = "#005a5d",
            Warning = "#FFC107",
            Error = "#F44336",
            AppbarBackground = "#00292a",
            AppbarText = "#FFFFFF",
            Background = "#121212",
            Surface = "#1E1E1E",
            TextPrimary = "#E0E0E0",
            TextSecondary = "#B0BEC5",
            ActionDefault = "#2D8B8B",
            DrawerBackground = "#1E1E1E",
            DrawerText = "#E0E0E0",
            LinesDefault = "rgba(0,0,0,0)",
            TableLines = "rgba(0,0,0,0)",
            Divider = "rgba(0,0,0,0)",
            OverlayDark = "rgba(0, 90, 93, 0.6)"
        };

        // Typography - Modifying existing instances to avoid abstract class instantiation issues
        // Default
        theme.Typography.Default.FontFamily = new[] { "Roboto", "Helvetica", "Arial", "sans-serif" };
        theme.Typography.Default.FontSize = "1rem";
        theme.Typography.Default.FontWeight = "400"; // Changed to string
        theme.Typography.Default.LineHeight = "1.67"; // Double is accepted? Wait, error said double to string? Checking error 62,30: double to string. So string.
        // Wait, BaseTypography.LineHeight defined as double in older versions?
        // Error says: Cannot implicitly convert type 'double' to 'string'. So it wants string?
        // Let's use string.
        theme.Typography.Default.LineHeight = "1.67";
        theme.Typography.Default.LetterSpacing = ".00938em";

        // H1
        theme.Typography.H1.FontFamily = new[] { "League Gothic", "sans-serif" };
        theme.Typography.H1.TextTransform = "uppercase";
        theme.Typography.H1.FontSize = "3.6rem";
        theme.Typography.H1.FontWeight = "400";

        // H2
        theme.Typography.H2.FontFamily = new[] { "League Gothic", "sans-serif" };
        theme.Typography.H2.TextTransform = "uppercase";
        theme.Typography.H2.FontSize = "2.4rem";
        theme.Typography.H2.FontWeight = "400";

        // H3
        theme.Typography.H3.FontFamily = new[] { "League Gothic", "sans-serif" };
        theme.Typography.H3.TextTransform = "uppercase";
        theme.Typography.H3.FontSize = "1.8rem";
        theme.Typography.H3.FontWeight = "400";

        // H4
        theme.Typography.H4.FontFamily = new[] { "League Gothic", "sans-serif" };
        theme.Typography.H4.TextTransform = "uppercase";
        theme.Typography.H4.FontSize = "1.4rem";
        theme.Typography.H4.FontWeight = "400";

        // H5
        theme.Typography.H5.FontFamily = new[] { "League Gothic", "sans-serif" };
        theme.Typography.H5.TextTransform = "uppercase";
        theme.Typography.H5.FontSize = "1rem";
        theme.Typography.H5.FontWeight = "400";

        // H6
        theme.Typography.H6.FontFamily = new[] { "League Gothic", "sans-serif" };
        theme.Typography.H6.TextTransform = "uppercase";
        theme.Typography.H6.FontSize = "0.8rem";
        theme.Typography.H6.FontWeight = "400";

        // Button
        theme.Typography.Button.FontFamily = new[] { "League Gothic", "sans-serif" };
        theme.Typography.Button.TextTransform = "uppercase";
        theme.Typography.Button.FontSize = "1.2rem";
        theme.Typography.Button.FontWeight = "400";

        // Body1
        theme.Typography.Body1.FontFamily = new[] { "Roboto", "sans-serif" };
        theme.Typography.Body1.FontSize = "1rem";
        theme.Typography.Body1.LineHeight = "1.67";

        // Body2
        theme.Typography.Body2.FontFamily = new[] { "Roboto", "sans-serif" };
        theme.Typography.Body2.FontSize = "0.875rem";
        theme.Typography.Body2.LineHeight = "1.67";

        // Layout
        theme.LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "0px"
        };

        // Shadows
        // theme.Shadows = ... (keeping defaults)

        return theme;
    }
}
