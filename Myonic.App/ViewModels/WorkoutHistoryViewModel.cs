using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Repositories;
using Myonic.Pages;

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

    [ObservableProperty]
    private bool _hasNoHistory;

    public WorkoutHistoryViewModel(IWorkoutSessionRepository repository)
    {
        _repository = repository;
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        var items = await _repository.GetPastSessionsAsync();
        PastSessions.Clear();

        foreach (var s in items)
        {
            var duration = s.FinishedAt - s.StartedAt;
            var durationStr = duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours} ч {duration.Minutes} мин"
                : $"{Math.Max(1, (int)duration.TotalMinutes)} мин";

            PastSessions.Add(new PastWorkoutItem
            {
                Id = s.Id,
                Title = s.Title,
                DateText = s.StartedAt.ToLocalTime().ToString("d MMMM yyyy, HH:mm"),
                DurationText = durationStr,
                SummaryText = $"Упражнений: {s.ExerciseCount} • Подходов: {s.CompletedSetsCount}"
            });
        }

        HasNoHistory = PastSessions.Count == 0;
    }

    [RelayCommand]
    public async Task OpenSessionDetailAsync(PastWorkoutItem? item)
    {
        if (item == null) return;
        await Shell.Current.GoToAsync($"{nameof(WorkoutSessionDetailPage)}?sessionId={item.Id}");
    }
}
