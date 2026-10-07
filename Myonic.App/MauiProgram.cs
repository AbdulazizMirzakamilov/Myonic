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
            builder.Services.AddSingleton<IExerciseRepository, ExerciseRepository>();
            builder.Services.AddTransient<ExercisesViewModel>();
            builder.Services.AddTransient<ExerciseEditViewModel>();
            builder.Services.AddTransient<ExercisesPage>();
            builder.Services.AddTransient<ExerciseEditPage>();

            return builder.Build();
        }
    }
}
