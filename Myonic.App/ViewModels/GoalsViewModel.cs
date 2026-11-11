using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Pages;
using Myonic.Services;

namespace Myonic.ViewModels;

public partial class GoalItemViewModel : ObservableObject
{
    public int Id { get; }
    public string Title { get; }
    public GoalType Type { get; }
    public string TypeBadge { get; }
    public string TypeIcon { get; }
    public string Unit { get; }
    public double StartValue { get; }
    public double TargetValue { get; }
    public double CurrentValue { get; }
    public double ProgressFraction { get; }
    public int ProgressPercent { get; }
    public string ProgressPercentText { get; }
    public string ValuesSummaryText { get; }
    public string RemainingText { get; }
    public bool HasTargetDate { get; }
    public string? TargetDateText { get; }
    public bool IsAchieved { get; }
    public bool IsActive { get; }
    public bool HasNotes { get; }
    public string? Notes { get; }

    public GoalItemViewModel(Goal goal)
    {
        Id = goal.Id;
        Title = string.IsNullOrWhiteSpace(goal.Title)
            ? (goal.Type switch
            {
                GoalType.ExerciseWeight => goal.ExerciseName ?? "Силовая цель",
                GoalType.BodyWeight => "Вес тела",
                GoalType.BodyMeasurement => goal.BodyPart ?? "Замер тела",
                GoalType.WorkoutsFrequency => "Регулярность тренировок",
                _ => "Цель"
            })
            : goal.Title;

        Type = goal.Type;
        TypeIcon = goal.Type switch
        {
            GoalType.ExerciseWeight => "🏋️‍♂️",
            GoalType.BodyWeight => "⚖️",
            GoalType.BodyMeasurement => "📏",
            GoalType.WorkoutsFrequency => "📅",
            _ => "🎯"
        };

        TypeBadge = goal.Type switch
        {
            GoalType.ExerciseWeight => "СИЛА",
            GoalType.BodyWeight => "ВЕС ТЕЛА",
            GoalType.BodyMeasurement => "ЗАМЕР",
            GoalType.WorkoutsFrequency => "РЕГУЛЯРНОСТЬ",
            _ => "ЦЕЛЬ"
        };

        Unit = goal.Type switch
        {
            GoalType.ExerciseWeight => "кг",
            GoalType.BodyWeight => "кг",
            GoalType.BodyMeasurement => "см",
            GoalType.WorkoutsFrequency => "тр.",
            _ => ""
        };

        StartValue = goal.StartValue;
        TargetValue = goal.TargetValue;
        CurrentValue = goal.CurrentValue;
        ProgressFraction = goal.ProgressFraction;
        ProgressPercent = goal.ProgressPercent;
        ProgressPercentText = $"{goal.ProgressPercent}%";

        ValuesSummaryText = $"{StartValue:F1} → {TargetValue:F1} {Unit} (сейчас {CurrentValue:F1})";

        IsAchieved = goal.Status == GoalStatus.Achieved || goal.IsTargetMet;
        IsActive = goal.Status == GoalStatus.Active;

        if (IsAchieved)
        {
            RemainingText = "Выполнено! 🎉";
        }
        else
        {
            var remaining = goal.RemainingValue;
            RemainingText = remaining > 0
                ? $"Осталось: {remaining:F1} {Unit}"
                : "Цель достигнута! 🎉";
        }

        HasTargetDate = goal.TargetDate.HasValue;
        TargetDateText = goal.TargetDate.HasValue
            ? $"до {goal.TargetDate.Value:d MMMM yyyy}"
            : null;

        HasNotes = !string.IsNullOrWhiteSpace(goal.Notes);
        Notes = goal.Notes;
    }
}

public partial class GoalsViewModel : ObservableObject, IHasActiveWorkoutTracker
{
    private readonly IGoalRepository _goalRepo;
    public IActiveWorkoutTracker? ActiveWorkoutTracker { get; }

    public GoalsViewModel(
        IGoalRepository goalRepo,
        IActiveWorkoutTracker? activeWorkoutTracker = null)
    {
        _goalRepo = goalRepo;
        ActiveWorkoutTracker = activeWorkoutTracker;
    }

    public ObservableCollection<GoalItemViewModel> ActiveGoals { get; } = [];
    public ObservableCollection<GoalItemViewModel> CompletedGoals { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasActiveGoals;

    [ObservableProperty]
    private bool _hasNoActiveGoals = true;

    [ObservableProperty]
    private bool _hasCompletedGoals;

    [ObservableProperty]
    private string _activeCountText = "0 активных целей";

    [ObservableProperty]
    private string _completedCountText = "0 выполнено";

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await _goalRepo.SyncCurrentValuesAsync();
            var allGoals = await _goalRepo.GetAllGoalsAsync();

            ActiveGoals.Clear();
            CompletedGoals.Clear();

            int activeCount = 0;
            int completedCount = 0;

            foreach (var goal in allGoals)
            {
                var item = new GoalItemViewModel(goal);
                if (goal.Status == GoalStatus.Active && !goal.IsTargetMet)
                {
                    ActiveGoals.Add(item);
                    activeCount++;
                }
                else
                {
                    CompletedGoals.Add(item);
                    completedCount++;
                }
            }

            HasActiveGoals = activeCount > 0;
            HasNoActiveGoals = activeCount == 0;
            HasCompletedGoals = completedCount > 0;

            ActiveCountText = $"{activeCount} {GetGoalDeclension(activeCount)}";
            CompletedCountText = $"{completedCount} выполнено";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GoalsViewModel] LoadAsync error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string GetGoalDeclension(int count)
    {
        int rem100 = count % 100;
        int rem10 = count % 10;
        if (rem100 is >= 11 and <= 19) return "активных целей";
        if (rem10 == 1) return "активная цель";
        if (rem10 is >= 2 and <= 4) return "активные цели";
        return "активных целей";
    }

    [RelayCommand]
    public async Task AddGoalAsync()
    {
        await Shell.Current.GoToAsync(nameof(GoalEditPage));
    }

    [RelayCommand]
    public async Task EditGoalAsync(GoalItemViewModel item)
    {
        if (item == null) return;
        await Shell.Current.GoToAsync($"{nameof(GoalEditPage)}?id={item.Id}");
    }

    [RelayCommand]
    public async Task DeleteGoalAsync(GoalItemViewModel item)
    {
        if (item == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Удаление цели",
            $"Вы уверены, что хотите удалить цель «{item.Title}»?",
            "Удалить",
            "Отмена");

        if (!confirm) return;

        await _goalRepo.DeleteAsync(item.Id);
        await LoadAsync();
    }
}
