#if ANDROID
using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Myonic.Services;

namespace Myonic.Platforms.Android;

public class CustomShellRenderer : ShellRenderer
{
    public CustomShellRenderer(Context context) : base(context)
    {
    }

    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
    {
        return new CustomBottomNavViewAppearanceTracker(this, shellItem);
    }
}

public class CustomBottomNavViewAppearanceTracker : ShellBottomNavViewAppearanceTracker
{
    private BottomNavigationView? _bottomView;

    public CustomBottomNavViewAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
        : base(shellContext, shellItem)
    {
        // Listen to dynamic theme changes
        var themeService = IPlatformApplication.Current?.Services.GetService<IThemeService>();
        if (themeService != null)
        {
            themeService.ThemeChanged += isDark =>
            {
                if (_bottomView != null && _bottomView.IsAttachedToWindow)
                {
                    _bottomView.Post(() => ApplyThemeColors(_bottomView, isDark));
                }
            };
        }
    }

    public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        base.SetAppearance(bottomView, appearance);
        _bottomView = bottomView;

        var density = bottomView.Context?.Resources?.DisplayMetrics?.Density ?? 1.0f;

        // 1. Force labeled mode (all 5 icons and titles always visible simultaneously)
        bottomView.LabelVisibilityMode = LabelVisibilityMode.LabelVisibilityLabeled;

        // 2. Custom Typography: 10.5sp ensures labels (like "ПРОФИЛЬ", "ТРЕНИНГ") NEVER truncate
        bottomView.ItemTextAppearanceActive = Resource.Style.BottomNavTextActive;
        bottomView.ItemTextAppearanceInactive = Resource.Style.BottomNavTextInactive;

        // 3. Floating Margins & Elevation
        if (bottomView.LayoutParameters is ViewGroup.MarginLayoutParams lp)
        {
            int marginH = (int)(10 * density);
            int marginB = (int)(10 * density);
            lp.SetMargins(marginH, 0, marginH, marginB);
            lp.Height = (int)(64 * density);
            bottomView.LayoutParameters = lp;
        }

        bottomView.Elevation = 14 * density;

        // 4. Apply colors according to current theme
        bool isDark = Application.Current?.UserAppTheme != AppTheme.Light;
        ApplyThemeColors(bottomView, isDark);
    }

    private static void ApplyThemeColors(BottomNavigationView bottomView, bool isDark)
    {
        var density = bottomView.Context?.Resources?.DisplayMetrics?.Density ?? 1.0f;

        var activeHex = isDark ? "#CCFF00" : "#65A30D";
        var inactiveHex = isDark ? "#94A3B8" : "#64748B";
        var bgHex = isDark ? "#171A21" : "#FFFFFF";
        var borderHex = isDark ? "#2A313E" : "#CBD5E1";
        var indicatorHex = isDark ? "#24CCFF00" : "#2465A30D";
        var rippleHex = isDark ? "#33CCFF00" : "#2265A30D";

        var states = new int[][]
        {
            new int[] { global::Android.Resource.Attribute.StateChecked },
            new int[] { -global::Android.Resource.Attribute.StateChecked }
        };
        var colors = new int[]
        {
            global::Android.Graphics.Color.ParseColor(activeHex),
            global::Android.Graphics.Color.ParseColor(inactiveHex)
        };
        var colorStateList = new ColorStateList(states, colors);

        bottomView.ItemIconTintList = colorStateList;
        bottomView.ItemTextColor = colorStateList;
        bottomView.ItemActiveIndicatorColor = ColorStateList.ValueOf(global::Android.Graphics.Color.ParseColor(indicatorHex));

        var shape = new GradientDrawable();
        shape.SetShape(ShapeType.Rectangle);
        shape.SetCornerRadius(20 * density);
        shape.SetColor(global::Android.Graphics.Color.ParseColor(bgHex));
        shape.SetStroke((int)(1.2f * density), global::Android.Graphics.Color.ParseColor(borderHex));
        bottomView.Background = shape;

        bottomView.ItemRippleColor = ColorStateList.ValueOf(global::Android.Graphics.Color.ParseColor(rippleHex));
    }
}
#endif
