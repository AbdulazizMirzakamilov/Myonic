namespace Myonic
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Pages.ExerciseEditPage), typeof(Pages.ExerciseEditPage));
        }
    }
}
