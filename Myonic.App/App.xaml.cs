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
#endif

            return window;
        }
    }
}
