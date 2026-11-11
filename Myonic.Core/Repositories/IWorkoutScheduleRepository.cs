using Myonic.Core.Models;

namespace Myonic.Core.Repositories;

public class PlannedExerciseItem
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string PlanText { get; set; } = string.Empty;
}

public class PlannedWorkoutSummary
{
    public int? ScheduledWorkoutId { get; set; }
    public int ProgramWorkoutId { get; set; }
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string WorkoutName { get; set; } = string.Empty;
    public DayOfWeek? DayOfWeek { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? OriginalDate { get; set; }
    public ScheduledWorkoutStatus Status { get; set; } = ScheduledWorkoutStatus.Planned;
    public int ExerciseCount { get; set; }
    public List<PlannedExerciseItem> Exercises { get; set; } = [];
    public bool HasExercises => Exercises != null && Exercises.Count > 0;
}

public interface IWorkoutScheduleRepository
{
    Task<IReadOnlyList<PlannedWorkoutSummary>> GetPlannedWorkoutsForDateAsync(DateTime date);
    Task<IReadOnlyList<PlannedWorkoutSummary>> GetAllAvailableProgramWorkoutsAsync();
    Task<(IReadOnlySet<DayOfWeek> DefaultDays, IReadOnlySet<DateTime> ScheduledDates)> GetPlannedDayIndicatorsForWeekAsync(DateTime fromDate, DateTime toDate);
    Task<(IReadOnlySet<DateTime> MovedAwayDates, IReadOnlySet<DateTime> CancelledDates)> GetScheduleOverridesForRangeAsync(DateTime fromDate, DateTime toDate);
    Task<ScheduledWorkout> RescheduleWorkoutAsync(int programWorkoutId, DateTime originalDate, DateTime newDate);
    Task<ScheduledWorkout> ScheduleWorkoutAsync(int programWorkoutId, DateTime targetDate);
    Task CancelScheduledWorkoutAsync(int programWorkoutId, DateTime date);
    Task EnsureScheduledWorkoutsForRangeAsync(DateTime fromDate, DateTime toDate);
    Task<ScheduledWorkout?> GetScheduledWorkoutForDateAsync(int programWorkoutId, DateTime date);
}
