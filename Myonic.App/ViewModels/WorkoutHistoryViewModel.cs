using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Drawables;
using Myonic.Pages;
using Myonic.Services;

namespace Myonic.ViewModels;

public sealed class PastWorkoutItem
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string DateText { get; init; } = string.Empty;
    public string DurationText { get; init; } = string.Empty;
    public string SummaryText { get; init; } = string.Empty;
}

public partial class WorkoutHistoryViewModel : ObservableObject
{
    private readonly IWorkoutSessionRepository _repository;

    public ObservableCollection<PastWorkoutItem> PastSessions { get; } = [];
    public ObservableCollection<MuscleGroupDistributionItem> MuscleDistribution { get; } = [];

    public WeeklyVolumeBarDrawable VolumeBarDrawable { get; } = new();
    public WeeklyFrequencyDrawable FrequencyDrawable { get; } = new();

    public event Action? RequestChartsRedraw;

    public IActiveWorkoutTracker ActiveWorkoutTracker { get; }

    [ObservableProperty]
    private bool _isHistoryMode = true;

    public bool IsAnalyticsMode => !IsHistoryMode;

    [ObservableProperty]
    private bool _hasNoHistory;

    [ObservableProperty]
    private AnalyticsPeriod _selectedPeriod = AnalyticsPeriod.Days30;

    public bool IsPeriod30 => SelectedPeriod == AnalyticsPeriod.Days30;
    public bool IsPeriod90 => SelectedPeriod == AnalyticsPeriod.Days90;
    public bool IsPeriod180 => SelectedPeriod == AnalyticsPeriod.Days180;
    public bool IsPeriodAll => SelectedPeriod == AnalyticsPeriod.All;

    [ObservableProperty]
    private int _totalWorkouts;

    [ObservableProperty]
    private string _totalVolumeText = "0 кг";

    [ObservableProperty]
    private int _totalSets;

    [ObservableProperty]
    private int _totalReps;

    [ObservableProperty]
    private string _avgDurationText = "–";

    [ObservableProperty]
    private string _workoutsPerWeekText = "0";

    [ObservableProperty]
    private int _personalRecordsCount;

    [ObservableProperty]
    private int _compoundSetsCount;

    [ObservableProperty]
    private int _isolationSetsCount;

    [ObservableProperty]
    private bool _hasAnalyticsData;

    public bool HasNoAnalyticsData => !HasAnalyticsData;

    partial void OnHasAnalyticsDataChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoAnalyticsData));
    }

    [ObservableProperty]
    private bool _isLoadingAnalytics;

    public WorkoutHistoryViewModel(
        IWorkoutSessionRepository repository,
        IActiveWorkoutTracker? activeWorkoutTracker = null)
    {
        _repository = repository;
        ActiveWorkoutTracker = activeWorkoutTracker ?? new ActiveWorkoutTracker(repository);
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        await ActiveWorkoutTracker.RefreshAsync();
        await LoadAnalyticsAsync();
    }

    [RelayCommand]
    public void SelectHistoryMode()
    {
        IsHistoryMode = true;
        OnPropertyChanged(nameof(IsAnalyticsMode));
    }

    [RelayCommand]
    public async Task SelectAnalyticsModeAsync()
    {
        IsHistoryMode = false;
        OnPropertyChanged(nameof(IsAnalyticsMode));
        await LoadAnalyticsAsync();
    }

    [RelayCommand]
    public async Task SetPeriod30Async() => await ChangePeriodAsync(AnalyticsPeriod.Days30);

    [RelayCommand]
    public async Task SetPeriod90Async() => await ChangePeriodAsync(AnalyticsPeriod.Days90);

    [RelayCommand]
    public async Task SetPeriod180Async() => await ChangePeriodAsync(AnalyticsPeriod.Days180);

    [RelayCommand]
    public async Task SetPeriodAllAsync() => await ChangePeriodAsync(AnalyticsPeriod.All);

    private async Task ChangePeriodAsync(AnalyticsPeriod period)
    {
        SelectedPeriod = period;
        NotifyPeriodSelection();
        await LoadAnalyticsAsync();
    }

    private void NotifyPeriodSelection()
    {
        OnPropertyChanged(nameof(IsPeriod30));
        OnPropertyChanged(nameof(IsPeriod90));
        OnPropertyChanged(nameof(IsPeriod180));
        OnPropertyChanged(nameof(IsPeriodAll));
    }

    [RelayCommand]
    public async Task LoadAnalyticsAsync()
    {
        if (IsLoadingAnalytics) return;
        IsLoadingAnalytics = true;

        try
        {
            var summary = await _repository.GetAnalyticsSummaryAsync(SelectedPeriod);

            TotalWorkouts = summary.TotalWorkouts;
            TotalVolumeText = summary.FormattedTotalVolume;
            TotalSets = summary.TotalSets;
            TotalReps = summary.TotalReps;
            AvgDurationText = summary.FormattedAverageDuration;
            WorkoutsPerWeekText = $"{summary.WorkoutsPerWeek:F1}";
            PersonalRecordsCount = summary.PersonalRecordsCount;
            CompoundSetsCount = summary.CompoundSetsCount;
            IsolationSetsCount = summary.IsolationSetsCount;

            HasAnalyticsData = summary.TotalWorkouts > 0;

            VolumeBarDrawable.Points = summary.WeeklyPoints;
            FrequencyDrawable.Points = summary.WeeklyPoints;

            MuscleDistribution.Clear();
            foreach (var item in summary.MuscleDistribution)
            {
                MuscleDistribution.Add(item);
            }

            RequestChartsRedraw?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WorkoutHistoryViewModel] LoadAnalyticsAsync error: {ex}");
        }
        finally
        {
            IsLoadingAnalytics = false;
        }
    }

    [RelayCommand]
    public async Task OpenSessionDetailAsync(PastWorkoutItem? item)
    {
        if (item == null) return;
        await Shell.Current.GoToAsync($"{nameof(WorkoutSessionDetailPage)}?sessionId={item.Id}");
    }
}
