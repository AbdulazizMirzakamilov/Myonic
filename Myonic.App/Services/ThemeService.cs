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

        var app = Application.Current;
        if (app != null)
        {
            // Listen to OS system theme changes in real time for Auto/System mode
            app.RequestedThemeChanged += (s, e) =>
            {
                if (CurrentThemeMode == AppThemeMode.System)
                {
                    ApplyTheme(AppThemeMode.System);
                }
            };
        }

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

        bool dark;
        if (mode == AppThemeMode.System)
        {
            app.UserAppTheme = AppTheme.Unspecified;
            dark = app.RequestedTheme != AppTheme.Light;
        }
        else
        {
            dark = mode == AppThemeMode.Dark;
            app.UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;
        }

        IsDark = dark;

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
            // Deep contrast, warm slate foundation, premium lime/emerald button with sharp dark text
            res["KineticCanvas"] = Color.FromArgb("#F1F5F9");
            res["KineticSurface"] = Color.FromArgb("#FFFFFF");
            res["KineticWell"] = Color.FromArgb("#E2E8F0");
            res["KineticSurfaceHigh"] = Color.FromArgb("#CBD5E1");
            res["KineticBorder"] = Color.FromArgb("#CBD5E1");
            res["KineticBorderSubtle"] = Color.FromArgb("#E2E8F0");

            res["KineticVolt"] = Color.FromArgb("#84CC16"); // Crisp vibrant lime
            res["KineticVoltMuted"] = Color.FromArgb("#65A30D");
            res["KineticVoltText"] = Color.FromArgb("#0F172A"); // Sharp dark text on volt button

            res["KineticEmerald"] = Color.FromArgb("#059669");
            res["KineticIceBlue"] = Color.FromArgb("#0284C7");
            res["KineticCrimson"] = Color.FromArgb("#E11D48");

            res["KineticText"] = Color.FromArgb("#0F172A"); // High-contrast slate-900
            res["KineticTextSecondary"] = Color.FromArgb("#334155"); // Clear slate-700
            res["KineticTextMuted"] = Color.FromArgb("#64748B"); // Slate-500
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
#elif WINDOWS
        UpdateWindowsTitleBar(dark);
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
                : Android.Graphics.Color.ParseColor("#F1F5F9");

#pragma warning disable CA1422
            window.SetStatusBarColor(barColor);
            window.SetNavigationBarColor(barColor);
#pragma warning restore CA1422

            var insetsController = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
            if (insetsController != null)
            {
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

#if WINDOWS
    private static void UpdateWindowsTitleBar(bool isDark)
    {
        try
        {
            var window = Application.Current?.Windows?.FirstOrDefault();
            if (window != null)
            {
                App.ApplyWindowsTitleBar(window, isDark);
            }
        }
        catch
        {
        }
    }
#endif
}
