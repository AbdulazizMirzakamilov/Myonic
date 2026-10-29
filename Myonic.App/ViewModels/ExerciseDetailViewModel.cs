using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public sealed class ExerciseHistorySetDisplayItem
{
    public int SetNumber { get; init; }
    public string SummaryText { get; init; } = string.Empty;
    public string Est1RmText { get; init; } = string.Empty;
}

public sealed class ExerciseHistorySessionDisplayItem
{
    public int SessionId { get; init; }
    public string DateHeader { get; init; } = string.Empty;
    public string WorkoutTitle { get; init; } = string.Empty;
    public string SessionStatsText { get; init; } = string.Empty;
    public List<ExerciseHistorySetDisplayItem> Sets { get; init; } = [];
}

public partial class ExerciseDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutSessionRepository _sessionRepo;
    private static readonly CultureInfo RussianCulture = new("ru-RU");

    public ExerciseDetailViewModel(IWorkoutSessionRepository sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    [ObservableProperty]
    private int _exerciseId;

    [ObservableProperty]
    private string _exerciseName = string.Empty;

    [ObservableProperty]
    private string _exerciseTypeText = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _hasDescription;

    [ObservableProperty]
    private string _allTimeMaxWeightText = "—";

    [ObservableProperty]
    private string _allTimeBest1RmText = "—";

    [ObservableProperty]
    private string _totalSessionsText = "0";

    [ObservableProperty]
    private string _totalSetsText = "0";

    [ObservableProperty]
    private bool _hasHistory;

    [ObservableProperty]
    private bool _hasNoHistory = true;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<ExerciseHistorySessionDisplayItem> Sessions { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var val) && int.TryParse(val?.ToString(), out var id))
        {
            ExerciseId = id;
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (ExerciseId <= 0) return;

        IsLoading = true;
        try
        {
            var summary = await _sessionRepo.GetExerciseHistoryAsync(ExerciseId);

            ExerciseName = summary.ExerciseName;
            ExerciseTypeText = summary.ExerciseType.ToDisplayText();
            Description = summary.Description;
            HasDescription = !string.IsNullOrWhiteSpace(summary.Description);

            AllTimeMaxWeightText = summary.AllTimeMaxWeight > 0 ? $"{summary.AllTimeMaxWeight} кг" : "—";
            AllTimeBest1RmText = summary.AllTimeBest1Rm > 0 ? $"{summary.AllTimeBest1Rm} кг" : "—";
            TotalSessionsText = summary.TotalSessionsCount.ToString();
            TotalSetsText = summary.TotalSetsCount.ToString();
            HasHistory = summary.Sessions.Count > 0;
            HasNoHistory = !HasHistory;

            Sessions.Clear();
            foreach (var s in summary.Sessions)
            {
                var setItems = new List<ExerciseHistorySetDisplayItem>();
                foreach (var set in s.Sets)
                {
                    var rpeText = set.Rpe.HasValue ? $" @ RPE {set.Rpe.Value:0.#}" : string.Empty;
                    var est1RmText = set.Estimated1Rm > 0 ? $"1ПМ ~ {set.Estimated1Rm} кг" : string.Empty;

                    setItems.Add(new ExerciseHistorySetDisplayItem
                    {
                        SetNumber = set.SetNumber,
                        SummaryText = $"{set.Weight} кг × {set.Reps}{rpeText}",
                        Est1RmText = est1RmText
                    });
                }

                var statsText = s.TotalVolume > 0
                    ? $"Макс: {s.MaxWeight} кг  •  Объём: {s.TotalVolume:N0} кг"
                    : $"Макс: {s.MaxWeight} кг";

                Sessions.Add(new ExerciseHistorySessionDisplayItem
                {
                    SessionId = s.SessionId,
                    DateHeader = s.Date.ToLocalTime().ToString("d MMMM yyyy, dddd", RussianCulture),
                    WorkoutTitle = s.WorkoutTitle,
                    SessionStatsText = statsText,
                    Sets = setItems
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task EditExerciseAsync()
    {
        if (ExerciseId <= 0) return;
        await Shell.Current.GoToAsync($"{nameof(ExerciseEditPage)}?id={ExerciseId}");
    }
}
