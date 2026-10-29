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

        // 1. Explicitly scheduled workouts on this date
        var scheduledEntries = await db.Table<ScheduledWorkout>()
            .Where(sw => sw.Date == targetDate)
            .ToListAsync();

        var scheduledWorkoutIds = scheduledEntries.Select(sw => sw.ProgramWorkoutId).ToHashSet();

        // 2. Program workouts matching DefaultDayOfWeek
        var defaultDayWorkouts = await db.Table<ProgramWorkout>()
            .Where(pw => pw.DefaultDayOfWeek == dayOfWeek)
            .ToListAsync();

        var matchedWorkoutIds = new HashSet<int>(scheduledWorkoutIds);
        foreach (var dw in defaultDayWorkouts)
        {
            matchedWorkoutIds.Add(dw.Id);
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
        var workouts = await db.Table<ProgramWorkout>()
            .OrderBy(pw => pw.ProgramId)
            .ThenBy(pw => pw.Order)
            .ToListAsync();

        var result = new List<PlannedWorkoutSummary>();
        foreach (var w in workouts)
        {
            var summary = await BuildSummaryAsync(w.Id);
            if (summary != null)
                result.Add(summary);
        }

        return result;
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
