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
            return new Window(new AppShell());
        }
    }
}
