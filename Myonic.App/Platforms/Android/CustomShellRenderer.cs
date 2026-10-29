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
    public CustomBottomNavViewAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
        : base(shellContext, shellItem)
    {
    }

    public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        base.SetAppearance(bottomView, appearance);

        var density = bottomView.Context?.Resources?.DisplayMetrics?.Density ?? 1.0f;

        // 1. Force labeled mode (all 5 icons and titles always visible simultaneously)
        bottomView.LabelVisibilityMode = LabelVisibilityMode.LabelVisibilityLabeled;

        // 2. Custom Typography: 10.5sp ensures labels (like "ПРОФИЛЬ", "ТРЕНИНГ") NEVER truncate to "ПРОФИ..."
        bottomView.ItemTextAppearanceActive = Resource.Style.BottomNavTextActive;
        bottomView.ItemTextAppearanceInactive = Resource.Style.BottomNavTextInactive;

        // 3. ColorStateList for icons and text: #CCFF00 (KineticVolt) active, #94A3B8 inactive
        var states = new int[][]
        {
            new int[] { global::Android.Resource.Attribute.StateChecked },
            new int[] { -global::Android.Resource.Attribute.StateChecked }
        };
        var colors = new int[]
        {
            global::Android.Graphics.Color.ParseColor("#CCFF00"),
            global::Android.Graphics.Color.ParseColor("#94A3B8")
        };
        var colorStateList = new ColorStateList(states, colors);

        bottomView.ItemIconTintList = colorStateList;
        bottomView.ItemTextColor = colorStateList;

        // 4. Active Icon Indicator: subtle neon volt glow pill behind active icon
        bottomView.ItemActiveIndicatorColor = ColorStateList.ValueOf(global::Android.Graphics.Color.ParseColor("#24CCFF00"));

        // 5. Rounded Floating Dock Background
        var shape = new GradientDrawable();
        shape.SetShape(ShapeType.Rectangle);
        shape.SetCornerRadius(20 * density);
        shape.SetColor(global::Android.Graphics.Color.ParseColor("#171A21")); // KineticSurface
        shape.SetStroke((int)(1.2f * density), global::Android.Graphics.Color.ParseColor("#2A313E")); // KineticBorder
        bottomView.Background = shape;

        // 6. Floating Margins & Elevation (adapts to phone bezels and gesture bar)
        if (bottomView.LayoutParameters is ViewGroup.MarginLayoutParams lp)
        {
            int marginH = (int)(10 * density);
            int marginB = (int)(10 * density);
            lp.SetMargins(marginH, 0, marginH, marginB);
            lp.Height = (int)(64 * density);
            bottomView.LayoutParameters = lp;
        }

        bottomView.Elevation = 14 * density;

        // 7. Subtle neon ripple on tap
        bottomView.ItemRippleColor = ColorStateList.ValueOf(global::Android.Graphics.Color.ParseColor("#33CCFF00"));

        // IMPORTANT: Do NOT subscribe to bottomView.ItemSelected!
        // In Android .NET, subscribing to bottomView.ItemSelected assigns a new listener and overrides
        // MAUI Shell's ShellItemRenderer listener, breaking Shell page navigation completely.
        // MAUI's ShellItemRenderer must retain ownership of OnNavigationItemSelected.
    }
}
#endif
