namespace Myonic
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Pages.ExerciseEditPage), typeof(Pages.ExerciseEditPage));
            Routing.RegisterRoute(nameof(Pages.ProgramEditPage), typeof(Pages.ProgramEditPage));
            Routing.RegisterRoute(nameof(Pages.WorkoutEditPage), typeof(Pages.WorkoutEditPage));
        }
    }
}
