using Microsoft.Extensions.Logging;
using Myonic.Core.Repositories;
using Myonic.Data;
using Myonic.Data.Repositories;
using Myonic.Pages;
using Myonic.ViewModels;

namespace Myonic
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton(_ => new MyonicDatabase(
                Path.Combine(FileSystem.AppDataDirectory, "myonic.db3")));

            // Exercise Library
            builder.Services.AddSingleton<IExerciseRepository, ExerciseRepository>();
            builder.Services.AddTransient<ExercisesViewModel>();
            builder.Services.AddTransient<ExerciseEditViewModel>();
            builder.Services.AddTransient<ExercisesPage>();
            builder.Services.AddTransient<ExerciseEditPage>();

            // Training Programs & Workouts
            builder.Services.AddSingleton<ITrainingProgramRepository, TrainingProgramRepository>();
            builder.Services.AddTransient<ProgramsViewModel>();
            builder.Services.AddTransient<ProgramEditViewModel>();
            builder.Services.AddTransient<WorkoutEditViewModel>();
            builder.Services.AddTransient<ProgramsPage>();
            builder.Services.AddTransient<ProgramEditPage>();
            builder.Services.AddTransient<WorkoutEditPage>();

            // Program Exercises
            builder.Services.AddSingleton<IProgramExerciseRepository, ProgramExerciseRepository>();
            builder.Services.AddTransient<ExercisePickerViewModel>();
            builder.Services.AddTransient<ExercisePickerPage>();
            builder.Services.AddTransient<ProgramExerciseEditViewModel>();
            builder.Services.AddTransient<ProgramExerciseEditPage>();

            // 1RM Calculator
            builder.Services.AddTransient<OneRepMaxViewModel>();
            builder.Services.AddTransient<OneRepMaxPage>();

            // Live Workout Sessions & History
            builder.Services.AddSingleton<IWorkoutSessionRepository, WorkoutSessionRepository>();
            builder.Services.AddTransient<ActiveWorkoutViewModel>();
            builder.Services.AddTransient<ActiveWorkoutPage>();
            builder.Services.AddTransient<WorkoutHistoryViewModel>();
            builder.Services.AddTransient<WorkoutHistoryPage>();

            // Schedule & Unified Calendar
            builder.Services.AddSingleton<IWorkoutScheduleRepository, WorkoutScheduleRepository>();
            builder.Services.AddTransient<ScheduleViewModel>();
            builder.Services.AddTransient<SchedulePage>();

            return builder.Build();
        }
    }
}
