using Myonic.Core.Repositories;

namespace Myonic.Core.Calendar;

public enum CalendarWorkoutStatus
{
    Planned,
    Completed,
    Missed
}

public sealed class CalendarWorkoutItem
{
    public int? SessionId { get; init; }
    public int? ProgramWorkoutId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public string TimeText { get; init; } = string.Empty;
    public string DurationText { get; init; } = string.Empty;
    public CalendarWorkoutStatus Status { get; init; }
    public int ExercisesCount { get; init; }
    public int SetsCount { get; init; }
    public string SummaryText { get; init; } = string.Empty;
    public IReadOnlyList<PlannedExerciseItem> PlannedExercises { get; init; } = [];
    public IReadOnlyList<CalendarCompletedExerciseItem> CompletedExercises { get; init; } = [];

    public bool HasPlannedExercises => PlannedExercises != null && PlannedExercises.Count > 0;
    public bool HasCompletedExercises => CompletedExercises != null && CompletedExercises.Count > 0;

    public bool IsCompleted => Status == CalendarWorkoutStatus.Completed;
    public bool IsPlanned => Status == CalendarWorkoutStatus.Planned;
    public bool IsMissed => Status == CalendarWorkoutStatus.Missed;
    public bool CanStart => Status == CalendarWorkoutStatus.Planned || Status == CalendarWorkoutStatus.Missed;
    public bool CanViewDetails => Status == CalendarWorkoutStatus.Completed;

    public string StatusDisplayName => Status switch
    {
        CalendarWorkoutStatus.Completed => "Выполнена",
        CalendarWorkoutStatus.Planned => "Запланирована",
        CalendarWorkoutStatus.Missed => "Пропущена",
        _ => string.Empty
    };

    public string StatusIcon => Status switch
    {
        CalendarWorkoutStatus.Completed => "✓",
        CalendarWorkoutStatus.Planned => "📅",
        CalendarWorkoutStatus.Missed => "⚠",
        _ => string.Empty
    };
}

public sealed class CalendarCompletedExerciseItem
{
    public string ExerciseName { get; init; } = string.Empty;
    public IReadOnlyList<string> SetsSummary { get; init; } = [];
}

public sealed class CalendarDayIndicators
{
    public bool HasCompleted { get; init; }
    public bool HasPlanned { get; init; }
    public bool HasMissed { get; init; }

    public bool HasAny => HasCompleted || HasPlanned || HasMissed;
}

public interface ICalendarService
{
    Task<IReadOnlyList<CalendarWorkoutItem>> GetWorkoutsForDateAsync(DateTime date);
    Task<IReadOnlyDictionary<DateTime, CalendarDayIndicators>> GetIndicatorsForRangeAsync(DateTime fromDate, DateTime toDate);
}
