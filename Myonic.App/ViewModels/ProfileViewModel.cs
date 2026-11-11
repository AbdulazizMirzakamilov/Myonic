using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IWorkoutSessionRepository _sessionRepo;
    private readonly IPersonalRecordRepository _prRepo;
    private readonly ITrainingProgramRepository _programRepo;
    private readonly IBodyMeasurementRepository? _bodyMeasurementRepo;
    private readonly IGoalRepository? _goalRepo;

    public Myonic.Services.IActiveWorkoutTracker ActiveWorkoutTracker { get; }

    public ProfileViewModel(
        IWorkoutSessionRepository sessionRepo,
        IPersonalRecordRepository prRepo,
        ITrainingProgramRepository programRepo,
        IBodyMeasurementRepository? bodyMeasurementRepo = null,
        IGoalRepository? goalRepo = null,
        Myonic.Services.IActiveWorkoutTracker? activeWorkoutTracker = null)
    {
        _sessionRepo = sessionRepo;
        _prRepo = prRepo;
        _programRepo = programRepo;
        _bodyMeasurementRepo = bodyMeasurementRepo;
        _goalRepo = goalRepo;
        ActiveWorkoutTracker = activeWorkoutTracker ?? new Myonic.Services.ActiveWorkoutTracker(sessionRepo);
    }

    [ObservableProperty]
    private int _completedWorkoutsCount;

    [ObservableProperty]
    private int _personalRecordsCount;

    [ObservableProperty]
    private int _programsCount;

    [ObservableProperty]
    private string _latestWeightText = "–";

    [ObservableProperty]
    private string? _weightDeltaText;

    [ObservableProperty]
    private bool _hasWeightData;

    [ObservableProperty]
    private string _weightDateText = "";

    // Goals summary properties
    [ObservableProperty]
    private int _activeGoalsCount;

    [ObservableProperty]
    private int _achievedGoalsCount;

    [ObservableProperty]
    private bool _hasActiveGoals;

    [ObservableProperty]
    private bool _hasTopGoal;

    [ObservableProperty]
    private bool _hasNoTopGoal = true;

    [ObservableProperty]
    private string _topGoalTitle = "";

    [ObservableProperty]
    private string _topGoalProgressText = "";

    [ObservableProperty]
    private double _topGoalFraction;

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

            if (_bodyMeasurementRepo != null)
            {
                var latest = await _bodyMeasurementRepo.GetLatestAsync();
                if (latest != null && latest.WeightKg > 0)
                {
                    HasWeightData = true;
                    LatestWeightText = $"{latest.WeightKg:F1} кг";
                    WeightDateText = latest.Date.ToString("d MMMM", new System.Globalization.CultureInfo("ru-RU"));

                    var trend = await _bodyMeasurementRepo.GetWeightTrendStatsAsync(30);
                    if (trend.MeasurementsCount > 1)
                    {
                        var s = trend.DeltaKg >= 0 ? "+" : "";
                        WeightDeltaText = $"{s}{trend.DeltaKg:F1} кг (30 дн)";
                    }
                    else
                    {
                        WeightDeltaText = null;
                    }
                }
                else
                {
                    HasWeightData = false;
                    LatestWeightText = "–";
                    WeightDeltaText = null;
                    WeightDateText = "";
                }
            }

            if (_goalRepo != null)
            {
                await _goalRepo.SyncCurrentValuesAsync();
                var activeGoals = await _goalRepo.GetActiveGoalsAsync();
                var allGoals = await _goalRepo.GetAllGoalsAsync();

                ActiveGoalsCount = activeGoals.Count;
                AchievedGoalsCount = allGoals.Count(g => g.Status == GoalStatus.Achieved || g.IsTargetMet);
                HasActiveGoals = activeGoals.Count > 0;

                if (activeGoals.Count > 0)
                {
                    var top = activeGoals[0];
                    HasTopGoal = true;
                    HasNoTopGoal = false;
                    TopGoalTitle = top.Title;
                    TopGoalFraction = top.ProgressFraction;
                    TopGoalProgressText = $"{top.ProgressPercent}% ({top.CurrentValue:F1} / {top.TargetValue:F1})";
                }
                else
                {
                    HasTopGoal = false;
                    HasNoTopGoal = true;
                    TopGoalTitle = "";
                    TopGoalProgressText = "";
                    TopGoalFraction = 0;
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task OpenGoalsAsync()
    {
        await Shell.Current.GoToAsync(nameof(GoalsPage));
    }

    [RelayCommand]
    public async Task QuickAddGoalAsync()
    {
        await Shell.Current.GoToAsync(nameof(GoalEditPage));
    }

    [RelayCommand]
    public async Task OpenBodyMeasurementsAsync()
    {
        await Shell.Current.GoToAsync(nameof(BodyMeasurementsPage));
    }

    [RelayCommand]
    public async Task QuickAddWeightAsync()
    {
        await Shell.Current.GoToAsync(nameof(BodyMeasurementEditPage));
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
    public async Task OpenWorkoutHistoryAsync()
    {
        await Shell.Current.GoToAsync(nameof(WorkoutHistoryPage));
    }

    [RelayCommand]
    public async Task OpenAnalyticsAsync()
    {
        await Shell.Current.GoToAsync(nameof(WorkoutHistoryPage));
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
