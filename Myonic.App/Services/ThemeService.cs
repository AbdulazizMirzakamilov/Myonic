using Microsoft.Maui.Controls;

namespace Myonic.Services;

public class ThemeService : IThemeService
{
    private const string ThemePreferenceKey = "app_theme_mode";

    public AppThemeMode CurrentThemeMode { get; private set; } = AppThemeMode.Dark;
    public bool IsDark { get; private set; } = true;
    public event Action<bool>? ThemeChanged;

    public void Initialize()
    {
        var saved = Preferences.Get(ThemePreferenceKey, (int)AppThemeMode.Dark);
        CurrentThemeMode = Enum.IsDefined(typeof(AppThemeMode), saved)
            ? (AppThemeMode)saved
            : AppThemeMode.Dark;

        ApplyTheme(CurrentThemeMode);
    }

    public void SetThemeMode(AppThemeMode mode)
    {
        CurrentThemeMode = mode;
        Preferences.Set(ThemePreferenceKey, (int)mode);
        ApplyTheme(mode);
    }

    private void ApplyTheme(AppThemeMode mode)
    {
        var app = Application.Current;
        if (app == null) return;

        bool dark = mode switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            AppThemeMode.System => app.RequestedTheme != AppTheme.Light,
            _ => true
        };

        IsDark = dark;
        app.UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;

        var res = app.Resources;
        if (dark)
        {
            // Dark (Kinetic Obsidian) Palette
            res["KineticCanvas"] = Color.FromArgb("#0F1115");
            res["KineticSurface"] = Color.FromArgb("#171A21");
            res["KineticWell"] = Color.FromArgb("#1F242D");
            res["KineticSurfaceHigh"] = Color.FromArgb("#282A2E");
            res["KineticBorder"] = Color.FromArgb("#2A313E");
            res["KineticBorderSubtle"] = Color.FromArgb("#232832");

            res["KineticVolt"] = Color.FromArgb("#CCFF00");
            res["KineticVoltMuted"] = Color.FromArgb("#9AC600");
            res["KineticVoltText"] = Color.FromArgb("#0F1115");

            res["KineticEmerald"] = Color.FromArgb("#10B981");
            res["KineticIceBlue"] = Color.FromArgb("#38BDF8");
            res["KineticCrimson"] = Color.FromArgb("#FF385C");

            res["KineticText"] = Color.FromArgb("#FFFFFF");
            res["KineticTextSecondary"] = Color.FromArgb("#94A3B8");
            res["KineticTextMuted"] = Color.FromArgb("#64748B");
        }
        else
        {
            // Light (Clean Slate) Palette
            res["KineticCanvas"] = Color.FromArgb("#F8FAFC");
            res["KineticSurface"] = Color.FromArgb("#FFFFFF");
            res["KineticWell"] = Color.FromArgb("#F1F5F9");
            res["KineticSurfaceHigh"] = Color.FromArgb("#E2E8F0");
            res["KineticBorder"] = Color.FromArgb("#CBD5E1");
            res["KineticBorderSubtle"] = Color.FromArgb("#E2E8F0");

            res["KineticVolt"] = Color.FromArgb("#65A30D");
            res["KineticVoltMuted"] = Color.FromArgb("#4D7C0F");
            res["KineticVoltText"] = Color.FromArgb("#FFFFFF");

            res["KineticEmerald"] = Color.FromArgb("#059669");
            res["KineticIceBlue"] = Color.FromArgb("#0284C7");
            res["KineticCrimson"] = Color.FromArgb("#E11D48");

            res["KineticText"] = Color.FromArgb("#0F172A");
            res["KineticTextSecondary"] = Color.FromArgb("#475569");
            res["KineticTextMuted"] = Color.FromArgb("#64748B");
        }

        // Update Brushes
        res["KineticCanvasBrush"] = new SolidColorBrush((Color)res["KineticCanvas"]);
        res["KineticSurfaceBrush"] = new SolidColorBrush((Color)res["KineticSurface"]);
        res["KineticWellBrush"] = new SolidColorBrush((Color)res["KineticWell"]);
        res["KineticBorderBrush"] = new SolidColorBrush((Color)res["KineticBorder"]);
        res["KineticVoltBrush"] = new SolidColorBrush((Color)res["KineticVolt"]);
        res["KineticEmeraldBrush"] = new SolidColorBrush((Color)res["KineticEmerald"]);
        res["KineticIceBlueBrush"] = new SolidColorBrush((Color)res["KineticIceBlue"]);
        res["KineticCrimsonBrush"] = new SolidColorBrush((Color)res["KineticCrimson"]);
        res["KineticTextBrush"] = new SolidColorBrush((Color)res["KineticText"]);
        res["KineticTextSecondaryBrush"] = new SolidColorBrush((Color)res["KineticTextSecondary"]);
        res["KineticTextMutedBrush"] = new SolidColorBrush((Color)res["KineticTextMuted"]);

#if ANDROID
        UpdateAndroidSystemBars(dark);
#endif

        ThemeChanged?.Invoke(dark);
    }

#if ANDROID
    private static void UpdateAndroidSystemBars(bool isDark)
    {
        try
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            var window = activity?.Window;
            if (window == null) return;

            var barColor = isDark
                ? Android.Graphics.Color.ParseColor("#0F1115")
                : Android.Graphics.Color.ParseColor("#F8FAFC");

#pragma warning disable CA1422
            window.SetStatusBarColor(barColor);
            window.SetNavigationBarColor(barColor);
#pragma warning restore CA1422

            var insetsController = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
            if (insetsController != null)
            {
                // In light theme, status bar icons (battery, clock) must be dark.
                // In dark theme, status bar icons must be white/light.
                insetsController.AppearanceLightStatusBars = !isDark;
                insetsController.AppearanceLightNavigationBars = !isDark;
            }
        }
        catch
        {
            // Fallback for safety across OEM Android skins
        }
    }
#endif
}
