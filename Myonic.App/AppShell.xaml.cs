namespace Myonic
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Pages.ExerciseDetailPage), typeof(Pages.ExerciseDetailPage));
            Routing.RegisterRoute(nameof(Pages.ExercisesPage), typeof(Pages.ExercisesPage));
            Routing.RegisterRoute(nameof(Pages.PersonalRecordsPage), typeof(Pages.PersonalRecordsPage));
            Routing.RegisterRoute(nameof(Pages.ExerciseEditPage), typeof(Pages.ExerciseEditPage));
            Routing.RegisterRoute(nameof(Pages.ProgramEditPage), typeof(Pages.ProgramEditPage));
            Routing.RegisterRoute(nameof(Pages.WorkoutEditPage), typeof(Pages.WorkoutEditPage));
            Routing.RegisterRoute(nameof(Pages.ProgramExerciseEditPage), typeof(Pages.ProgramExerciseEditPage));
            Routing.RegisterRoute(nameof(Pages.ActiveWorkoutPage), typeof(Pages.ActiveWorkoutPage));
            Routing.RegisterRoute(nameof(Pages.WorkoutSessionDetailPage), typeof(Pages.WorkoutSessionDetailPage));
            Routing.RegisterRoute(nameof(Pages.SettingsPage), typeof(Pages.SettingsPage));
        }
    }
}
