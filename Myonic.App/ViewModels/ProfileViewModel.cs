using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IWorkoutSessionRepository _sessionRepo;
    private readonly IPersonalRecordRepository _prRepo;
    private readonly ITrainingProgramRepository _programRepo;

    public Myonic.Services.IActiveWorkoutTracker ActiveWorkoutTracker { get; }

    public ProfileViewModel(
        IWorkoutSessionRepository sessionRepo,
        IPersonalRecordRepository prRepo,
        ITrainingProgramRepository programRepo,
        Myonic.Services.IActiveWorkoutTracker? activeWorkoutTracker = null)
    {
        _sessionRepo = sessionRepo;
        _prRepo = prRepo;
        _programRepo = programRepo;
        ActiveWorkoutTracker = activeWorkoutTracker ?? new Myonic.Services.ActiveWorkoutTracker(sessionRepo);
    }

    [ObservableProperty]
    private int _completedWorkoutsCount;

    [ObservableProperty]
    private int _personalRecordsCount;

    [ObservableProperty]
    private int _programsCount;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    public async Task LoadAsync()
    {
        await ActiveWorkoutTracker.RefreshAsync();
        IsLoading = true;
        try
        {
            var completedCountTask = _sessionRepo.GetCompletedSessionsCountAsync();
            var prsCountTask = _prRepo.GetCountAsync();
            var programsCountTask = _programRepo.GetCountAsync();

            await Task.WhenAll(completedCountTask, prsCountTask, programsCountTask);

            CompletedWorkoutsCount = await completedCountTask;
            PersonalRecordsCount = await prsCountTask;
            ProgramsCount = await programsCountTask;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task OpenExercisesAsync()
    {
        await Shell.Current.GoToAsync(nameof(ExercisesPage));
    }

    [RelayCommand]
    public async Task OpenPersonalRecordsAsync()
    {
        await Shell.Current.GoToAsync(nameof(PersonalRecordsPage));
    }

    [RelayCommand]
    public async Task OpenSettingsAsync()
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    [RelayCommand]
    public async Task OpenOneRepMaxAsync()
    {
        await Shell.Current.GoToAsync("//OneRepMaxPage");
    }

    [RelayCommand]
    public async Task OpenAboutAsync()
    {
        await Shell.Current.DisplayAlertAsync(
            "О приложении Myonic",
            "Myonic – трекер силовых тренировок и прогрессии нагрузок.\n\n" +
            "• Версия: 1.0 (MVP)\n" +
            "• Платформа: .NET 10 MAUI\n" +
            "• База данных: SQLite-net\n" +
            "• Дизайн-система: Kinetic Obsidian\n" +
            "• RPE & личные рекорды\n\n" +
            "Разработано для максимальной производительности и чёткого прогресса.",
            "OK");
    }
}
