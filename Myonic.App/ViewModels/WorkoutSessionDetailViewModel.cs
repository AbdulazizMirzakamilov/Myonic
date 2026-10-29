using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Analysis;
using Myonic.Core.Repositories;

namespace Myonic.ViewModels;

public sealed class SessionSetDisplayItem
{
    public int SetNumber { get; init; }
    public string SummaryText { get; init; } = string.Empty;
    public string? Comment { get; init; }
}

public sealed class ExercisePlanFactDisplayItem
{
    public int ExerciseId { get; init; }
    public string ExerciseName { get; init; } = string.Empty;
    public bool HasPlan { get; init; }
    public string PlanText { get; init; } = string.Empty;
    public string SetsComparisonText { get; init; } = string.Empty;
    public string RepsComparisonText { get; init; } = string.Empty;
    public string WeightComparisonText { get; init; } = string.Empty;
    public string VolumeComparisonText { get; init; } = string.Empty;
    public string? RpeComparisonText { get; init; }
    public bool HasRpeComparison => !string.IsNullOrEmpty(RpeComparisonText);
    public string StatusBadgeText { get; init; } = string.Empty;
    public string ComplianceColorHex { get; init; } = "#10B981";
    public List<SessionSetDisplayItem> Sets { get; init; } = [];
}

public partial class WorkoutSessionDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutSessionRepository _sessionRepo;
    private static readonly CultureInfo RussianCulture = new("ru-RU");

    public WorkoutSessionDetailViewModel(IWorkoutSessionRepository sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    [ObservableProperty]
    private int _sessionId;

    [ObservableProperty]
    private string _title = "Детали тренировки";

    [ObservableProperty]
    private string _dateFormatted = string.Empty;

    [ObservableProperty]
    private string _durationText = string.Empty;

    [ObservableProperty]
    private string? _comment;

    [ObservableProperty]
    private bool _hasComment;

    [ObservableProperty]
    private bool _hasPlan;

    [ObservableProperty]
    private string _overallStatusText = string.Empty;

    [ObservableProperty]
    private string _overallColorHex = "#10B981";

    [ObservableProperty]
    private int _overallCompliancePercent = 100;

    [ObservableProperty]
    private double _complianceProgress = 1.0;

    [ObservableProperty]
    private string _totalSetsText = string.Empty;

    [ObservableProperty]
    private string _totalVolumeText = string.Empty;

    [ObservableProperty]
    private string _totalRepsText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<ExercisePlanFactDisplayItem> Exercises { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("sessionId", out var val) && int.TryParse(val?.ToString(), out var sId) && sId > 0)
        {
            SessionId = sId;
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (SessionId <= 0) return;

        IsLoading = true;
        try
        {
            var detail = await _sessionRepo.GetSessionDetailAsync(SessionId);
            if (detail == null) return;

            Title = string.IsNullOrWhiteSpace(detail.Title) ? "Тренировка" : detail.Title;
            DateFormatted = detail.StartedAt.ToLocalTime().ToString("d MMMM yyyy, dddd (HH:mm)", RussianCulture);

            if (detail.FinishedAt.HasValue)
            {
                var duration = detail.FinishedAt.Value - detail.StartedAt;
                DurationText = duration.TotalHours >= 1
                    ? $"{(int)duration.TotalHours} ч {duration.Minutes} мин"
                    : $"{duration.Minutes} мин";
            }
            else
            {
                DurationText = "В процессе";
            }

            Comment = detail.Comment;
            HasComment = !string.IsNullOrWhiteSpace(detail.Comment);

            var summary = PlanFactAnalyzer.Analyze(detail);

            HasPlan = summary.HasPlan;
            OverallStatusText = summary.OverallStatusText;
            OverallColorHex = summary.OverallColorHex;
            OverallCompliancePercent = summary.OverallCompliancePercent;
            ComplianceProgress = Math.Clamp(summary.OverallCompliancePercent / 100.0, 0.0, 1.0);

            if (summary.HasPlan)
            {
                TotalSetsText = $"{summary.TotalActualSets} из {summary.TotalPlannedSets}";
                TotalRepsText = $"{summary.TotalActualReps} из {summary.TotalPlannedReps}";
                TotalVolumeText = summary.TotalPlannedVolume > 0
                    ? $"{summary.TotalActualVolume:N0} из {summary.TotalPlannedVolume:N0} кг"
                    : $"{summary.TotalActualVolume:N0} кг";
            }
            else
            {
                TotalSetsText = summary.TotalActualSets.ToString();
                TotalRepsText = summary.TotalActualReps.ToString();
                TotalVolumeText = $"{summary.TotalActualVolume:N0} кг";
            }

            Exercises.Clear();
            foreach (var an in summary.Exercises)
            {
                var sessionEx = detail.Exercises.FirstOrDefault(e => e.ExerciseId == an.ExerciseId);
                var setsList = new List<SessionSetDisplayItem>();
                if (sessionEx != null)
                {
                    foreach (var s in sessionEx.Sets)
                    {
                        var rpeText = s.Rpe.HasValue ? $" @ RPE {s.Rpe.Value:0.#}" : string.Empty;
                        setsList.Add(new SessionSetDisplayItem
                        {
                            SetNumber = s.SetNumber,
                            SummaryText = $"{s.Weight} кг × {s.Reps}{rpeText}",
                            Comment = s.Comment
                        });
                    }
                }

                string setsComparison = an.HasPlan
                    ? $"{an.ActualSets} / {an.PlannedSets} сетов"
                    : $"{an.ActualSets} сетов";

                string repsComparison = an.HasPlan
                    ? $"{an.ActualRepsTotal} / {an.PlannedRepsTotal} повт."
                    : $"{an.ActualRepsTotal} повт.";

                string weightComparison = an.PlannedWeight.HasValue
                    ? $"ср. {an.ActualAvgWeight} кг (план {an.PlannedWeight.Value} кг)"
                    : $"макс. {an.ActualMaxWeight} кг";

                string volumeComparison = an.PlannedVolume.HasValue
                    ? $"{an.ActualVolume:N0} / {an.PlannedVolume.Value:N0} кг"
                    : $"{an.ActualVolume:N0} кг";

                string? rpeComparison = null;
                if (an.PlannedRpe.HasValue || an.ActualAvgRpe.HasValue)
                {
                    var planPart = an.PlannedRpe.HasValue ? $"план {an.PlannedRpe.Value:0.#}" : "без плана";
                    var factPart = an.ActualAvgRpe.HasValue ? $"ср. {an.ActualAvgRpe.Value:0.#}" : "—";
                    rpeComparison = $"RPE: {factPart} ({planPart})";
                }

                Exercises.Add(new ExercisePlanFactDisplayItem
                {
                    ExerciseId = an.ExerciseId,
                    ExerciseName = an.ExerciseName,
                    HasPlan = an.HasPlan,
                    PlanText = an.PlanText,
                    SetsComparisonText = setsComparison,
                    RepsComparisonText = repsComparison,
                    WeightComparisonText = weightComparison,
                    VolumeComparisonText = volumeComparison,
                    RpeComparisonText = rpeComparison,
                    StatusBadgeText = an.StatusBadgeText,
                    ComplianceColorHex = an.ComplianceColorHex,
                    Sets = setsList
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
