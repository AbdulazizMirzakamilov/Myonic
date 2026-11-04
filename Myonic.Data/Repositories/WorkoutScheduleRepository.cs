using Myonic.Core.Formatting;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Data.Repositories;

public sealed class WorkoutScheduleRepository : IWorkoutScheduleRepository
{
    private readonly MyonicDatabase _database;

    public WorkoutScheduleRepository(MyonicDatabase database) => _database = database;

    public async Task<IReadOnlyList<PlannedWorkoutSummary>> GetPlannedWorkoutsForDateAsync(DateTime date)
    {
        var db = await _database.GetConnectionAsync();
        var targetDate = date.Date;
        var dayOfWeek = date.DayOfWeek;

        // 1. Explicitly scheduled workouts on this date (excluding cancelled)
        var scheduledEntries = await db.Table<ScheduledWorkout>()
            .Where(sw => sw.Date == targetDate && sw.Status != ScheduledWorkoutStatus.Cancelled)
            .ToListAsync();

        var scheduledWorkoutIds = scheduledEntries.Select(sw => sw.ProgramWorkoutId).ToHashSet();

        // 2. Program workouts matching DefaultDayOfWeek (only from non-archived programs)
        var defaultDayWorkouts = await db.QueryAsync<ProgramWorkout>(
            """
            SELECT pw.* FROM ProgramWorkout pw
            INNER JOIN TrainingProgram p ON p.Id = pw.ProgramId
            WHERE pw.DefaultDayOfWeek = ? AND p.IsArchived = 0
            """, (int)dayOfWeek);

        // Exclude workouts rescheduled away from targetDate or cancelled on targetDate
        var movedAwayIds = (await db.Table<ScheduledWorkout>()
            .Where(sw => sw.OriginalDate == targetDate && sw.Date != targetDate)
            .ToListAsync())
            .Select(sw => sw.ProgramWorkoutId)
            .ToHashSet();

        var cancelledIds = (await db.Table<ScheduledWorkout>()
            .Where(sw => sw.Date == targetDate && sw.Status == ScheduledWorkoutStatus.Cancelled)
            .ToListAsync())
            .Select(sw => sw.ProgramWorkoutId)
            .ToHashSet();

        var matchedWorkoutIds = new HashSet<int>(scheduledWorkoutIds);
        foreach (var dw in defaultDayWorkouts)
        {
            if (!movedAwayIds.Contains(dw.Id) && !cancelledIds.Contains(dw.Id))
            {
                matchedWorkoutIds.Add(dw.Id);
            }
        }

        var result = new List<PlannedWorkoutSummary>();
        foreach (var workoutId in matchedWorkoutIds)
        {
            var summary = await BuildSummaryAsync(workoutId);
            if (summary != null)
                result.Add(summary);
        }

        return result;
    }

    public async Task<IReadOnlyList<PlannedWorkoutSummary>> GetAllAvailableProgramWorkoutsAsync()
    {
        var db = await _database.GetConnectionAsync();
        var workouts = await db.QueryAsync<ProgramWorkout>(
            """
            SELECT pw.* FROM ProgramWorkout pw
            INNER JOIN TrainingProgram p ON p.Id = pw.ProgramId
            WHERE p.IsArchived = 0
            ORDER BY pw.ProgramId, pw."Order"
            """);

        var result = new List<PlannedWorkoutSummary>();
        foreach (var w in workouts)
        {
            var summary = await BuildSummaryAsync(w.Id);
            if (summary != null)
                result.Add(summary);
        }

        return result;
    }

    public async Task<(IReadOnlySet<DayOfWeek> DefaultDays, IReadOnlySet<DateTime> ScheduledDates)> GetPlannedDayIndicatorsForWeekAsync(DateTime fromDate, DateTime toDate)
    {
        var db = await _database.GetConnectionAsync();

        var defaultDaysRaw = await db.ExecuteScalarAsync<string>(
            """
            SELECT GROUP_CONCAT(DISTINCT pw.DefaultDayOfWeek)
            FROM ProgramWorkout pw
            INNER JOIN TrainingProgram p ON p.Id = pw.ProgramId
            WHERE pw.DefaultDayOfWeek IS NOT NULL AND p.IsArchived = 0
            """);

        var defaultDays = new HashSet<DayOfWeek>();
        if (!string.IsNullOrEmpty(defaultDaysRaw))
        {
            foreach (var part in defaultDaysRaw.Split(','))
            {
                if (int.TryParse(part, out var val))
                    defaultDays.Add((DayOfWeek)val);
            }
        }

        var scheduled = await db.Table<ScheduledWorkout>()
            .Where(sw => sw.Date >= fromDate.Date && sw.Date <= toDate.Date && sw.Status != ScheduledWorkoutStatus.Cancelled)
            .ToListAsync();

        var scheduledDates = new HashSet<DateTime>(scheduled.Select(s => s.Date.Date));

        return (defaultDays, scheduledDates);
    }

    private async Task<PlannedWorkoutSummary?> BuildSummaryAsync(int programWorkoutId)
    {
        var db = await _database.GetConnectionAsync();
        var workout = await db.FindAsync<ProgramWorkout>(programWorkoutId);
        if (workout == null) return null;

        var program = await db.FindAsync<TrainingProgram>(workout.ProgramId);
        var programName = program?.Name ?? "Программа";

        var exercises = await db.QueryAsync<ProgramExerciseRow>(
            """
            SELECT pe.Id, pe.ExerciseId, e.Name AS ExerciseName, pe."Order",
                   pe.Sets, pe.Reps, pe.RepsMax, pe.Weight, pe.Rpe, pe.RpeMax
            FROM ProgramExercise pe
            INNER JOIN Exercise e ON e.Id = pe.ExerciseId
            WHERE pe.ProgramWorkoutId = ?
            ORDER BY pe."Order" ASC
            """,
            programWorkoutId);

        var list = new List<PlannedExerciseItem>();
        foreach (var ex in exercises)
        {
            var planText = PlanTextFormatter.Format(
                ex.Sets,
                ex.Reps,
                ex.RepsMax,
                ex.Weight,
                ex.Rpe,
                ex.RpeMax);

            list.Add(new PlannedExerciseItem
            {
                ExerciseId = ex.ExerciseId,
                ExerciseName = ex.ExerciseName,
                PlanText = planText
            });
        }

        return new PlannedWorkoutSummary
        {
            ProgramWorkoutId = workout.Id,
            ProgramId = workout.ProgramId,
            ProgramName = programName,
            WorkoutName = workout.Name,
            DayOfWeek = workout.DefaultDayOfWeek,
            ExerciseCount = list.Count,
            Exercises = list
        };
    }

    private sealed class ProgramExerciseRow
    {
        public int Id { get; set; }
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int Order { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public int? RepsMax { get; set; }
        public double? Weight { get; set; }
        public double? Rpe { get; set; }
        public double? RpeMax { get; set; }
    }
}
