using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace Myonic
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Window != null)
            {
#pragma warning disable CA1422
                var obsidianColor = Android.Graphics.Color.ParseColor("#0F1115");
                Window.SetStatusBarColor(obsidianColor);
                Window.SetNavigationBarColor(obsidianColor);
#pragma warning restore CA1422

                // Allow seamless rendering around camera punch-holes / notches without awkward purple or black bars
                if (OperatingSystem.IsAndroidVersionAtLeast(28) && Window.Attributes != null)
                {
                    Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                }

                // White / light system icons (clock, battery, Wi-Fi) on our dark Obsidian canvas
                var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
                if (controller != null)
                {
                    controller.AppearanceLightStatusBars = false;
                    controller.AppearanceLightNavigationBars = false;
                }
            }
        }
    }
}
