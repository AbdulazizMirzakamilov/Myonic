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
    public int ProgramWorkoutId { get; set; }
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string WorkoutName { get; set; } = string.Empty;
    public DayOfWeek? DayOfWeek { get; set; }
    public int ExerciseCount { get; set; }
    public List<PlannedExerciseItem> Exercises { get; set; } = [];
    public bool HasExercises => Exercises != null && Exercises.Count > 0;
}

public interface IWorkoutScheduleRepository
{
    Task<IReadOnlyList<PlannedWorkoutSummary>> GetPlannedWorkoutsForDateAsync(DateTime date);
    Task<IReadOnlyList<PlannedWorkoutSummary>> GetAllAvailableProgramWorkoutsAsync();
    Task<(IReadOnlySet<DayOfWeek> DefaultDays, IReadOnlySet<DateTime> ScheduledDates)> GetPlannedDayIndicatorsForWeekAsync(DateTime fromDate, DateTime toDate);
}
