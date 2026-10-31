using Myonic.Services;

namespace Myonic
{
    public partial class App : Application
    {
        private readonly IThemeService _themeService;

        public App(IThemeService themeService)
        {
            InitializeComponent();
            _themeService = themeService;
            _themeService.Initialize();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS
            window.MinimumWidth = 980;
            window.MinimumHeight = 640;
            window.Width = 1200;
            window.Height = 800;
            window.Title = "Myonic";

            window.HandlerChanged += (s, e) =>
            {
                ApplyWindowsTitleBar(window, _themeService.IsDark);
            };

            window.Created += (s, e) =>
            {
                ApplyWindowsTitleBar(window, _themeService.IsDark);
            };

            _themeService.ThemeChanged += isDark =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ApplyWindowsTitleBar(window, isDark);
                });
            };
#endif

            return window;
        }

#if WINDOWS
        public static void ApplyWindowsTitleBar(Window window, bool isDark)
        {
            try
            {
                if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    if (nativeWindow.Content is Microsoft.UI.Xaml.FrameworkElement rootElement)
                    {
                        rootElement.RequestedTheme = isDark
                            ? Microsoft.UI.Xaml.ElementTheme.Dark
                            : Microsoft.UI.Xaml.ElementTheme.Light;
                    }

                    var handle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                    if (appWindow?.TitleBar != null && Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
                    {
                        var titleBar = appWindow.TitleBar;
                        // Light: #F1F5F9 canvas; Dark: #0F1115 canvas
                        var bg = isDark
                            ? global::Windows.UI.Color.FromArgb(255, 15, 17, 21)
                            : global::Windows.UI.Color.FromArgb(255, 241, 245, 249);
                        var fg = isDark
                            ? global::Windows.UI.Color.FromArgb(255, 255, 255, 255)
                            : global::Windows.UI.Color.FromArgb(255, 15, 23, 42);
                        var hoverBg = isDark
                            ? global::Windows.UI.Color.FromArgb(255, 31, 36, 45)
                            : global::Windows.UI.Color.FromArgb(255, 226, 232, 240);
                        var inactiveBg = bg;
                        var inactiveFg = isDark
                            ? global::Windows.UI.Color.FromArgb(255, 148, 163, 184)
                            : global::Windows.UI.Color.FromArgb(255, 100, 116, 139);

                        titleBar.BackgroundColor = bg;
                        titleBar.ForegroundColor = fg;
                        titleBar.ButtonBackgroundColor = bg;
                        titleBar.ButtonForegroundColor = fg;
                        titleBar.ButtonHoverBackgroundColor = hoverBg;
                        titleBar.ButtonHoverForegroundColor = fg;
                        titleBar.ButtonPressedBackgroundColor = hoverBg;
                        titleBar.ButtonPressedForegroundColor = fg;
                        titleBar.ButtonInactiveBackgroundColor = inactiveBg;
                        titleBar.ButtonInactiveForegroundColor = inactiveFg;
                        titleBar.InactiveBackgroundColor = inactiveBg;
                        titleBar.InactiveForegroundColor = inactiveFg;
                    }
                }
            }
            catch
            {
                // Fallback for safety
            }
        }
#endif
    }
}
