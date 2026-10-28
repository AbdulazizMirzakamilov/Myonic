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
            Routing.RegisterRoute(nameof(Pages.ExercisePickerPage), typeof(Pages.ExercisePickerPage));
            Routing.RegisterRoute(nameof(Pages.ProgramExerciseEditPage), typeof(Pages.ProgramExerciseEditPage));
        }
    }
}
