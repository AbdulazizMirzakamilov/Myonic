using Myonic.Core.Formatting;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Core.Validation;

namespace Myonic.Data.Repositories;

public sealed class WorkoutSessionRepository : IWorkoutSessionRepository
{
    private readonly MyonicDatabase _database;

    public WorkoutSessionRepository(MyonicDatabase database) => _database = database;

    public async Task<ActiveSessionSummary?> GetActiveSessionSummaryAsync()
    {
        var db = await _database.GetConnectionAsync();
        var session = await db.Table<WorkoutSession>()
            .Where(s => s.FinishedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

        if (session == null) return null;

        return new ActiveSessionSummary
        {
            Id = session.Id,
            Title = session.Title,
            StartedAt = session.StartedAt
        };
    }

    public async Task<int> StartSessionFromProgramWorkoutAsync(int programWorkoutId)
    {
        var db = await _database.GetConnectionAsync();

        var programWorkout = await db.FindAsync<ProgramWorkout>(programWorkoutId)
            ?? throw new ArgumentException($"Тренировка с ID {programWorkoutId} не найдена.");

        var session = new WorkoutSession
        {
            Title = programWorkout.Name,
            StartedAt = DateTime.UtcNow
        };
        await db.InsertAsync(session);

        var exercises = await db.Table<ProgramExercise>()
            .Where(pe => pe.ProgramWorkoutId == programWorkoutId)
            .OrderBy(pe => pe.Order)
            .ToListAsync();

        foreach (var pe in exercises)
        {
            var we = new WorkoutExercise
            {
                WorkoutSessionId = session.Id,
                ExerciseId = pe.ExerciseId,
                Order = pe.Order,
                PlannedSets = pe.Sets,
                PlannedReps = pe.Reps,
                PlannedRepsMax = pe.RepsMax,
                PlannedWeight = pe.Weight,
                PlannedRpe = pe.Rpe,
                PlannedRpeMax = pe.RpeMax,
                PlannedRestSeconds = pe.RestSeconds,
                Notes = pe.Notes
            };
            await db.InsertAsync(we);
        }

        return session.Id;
    }

    public async Task<WorkoutSessionDetail?> GetSessionDetailAsync(int sessionId)
    {
        var db = await _database.GetConnectionAsync();
        var session = await db.FindAsync<WorkoutSession>(sessionId);
        if (session == null) return null;

        var workoutExercises = await db.QueryAsync<WorkoutExerciseRow>(
            """
            SELECT we.Id, we.ExerciseId, e.Name AS ExerciseName, e.Type AS ExerciseType, we."Order",
                   we.PlannedSets, we.PlannedReps, we.PlannedRepsMax, we.PlannedWeight,
                   we.PlannedRpe, we.PlannedRpeMax, we.Notes
            FROM WorkoutExercise we
            INNER JOIN Exercise e ON e.Id = we.ExerciseId
            WHERE we.WorkoutSessionId = ?
            ORDER BY we."Order" ASC
            """,
            sessionId);

        var detail = new WorkoutSessionDetail
        {
            Id = session.Id,
            Title = session.Title,
            StartedAt = session.StartedAt,
            FinishedAt = session.FinishedAt,
            Comment = session.Comment
        };

        foreach (var row in workoutExercises)
        {
            var sets = await db.Table<SetEntry>()
                .Where(s => s.WorkoutExerciseId == row.Id)
                .OrderBy(s => s.SetNumber)
                .ToListAsync();

            var planText = string.Empty;
            if (row.PlannedSets.HasValue && row.PlannedReps.HasValue)
            {
                planText = PlanTextFormatter.Format(
                    row.PlannedSets.Value,
                    row.PlannedReps.Value,
                    row.PlannedRepsMax,
                    row.PlannedWeight,
                    row.PlannedRpe,
                    row.PlannedRpeMax);
            }

            detail.Exercises.Add(new SessionExerciseDetail
            {
                WorkoutExerciseId = row.Id,
                ExerciseId = row.ExerciseId,
                ExerciseName = row.ExerciseName,
                ExerciseType = (ExerciseType)row.ExerciseType,
                Order = row.Order,
                PlannedSets = row.PlannedSets,
                PlannedReps = row.PlannedReps,
                PlannedRepsMax = row.PlannedRepsMax,
                PlannedWeight = row.PlannedWeight,
                PlannedRpe = row.PlannedRpe,
                PlannedRpeMax = row.PlannedRpeMax,
                PlanText = planText,
                Sets = sets
            });
        }

        return detail;
    }

    public async Task<SetEntry> AddSetAsync(int workoutExerciseId, double weight, int reps, double? rpe, string? comment = null)
    {
        if (weight < 0)
            throw new ArgumentException("Вес не может быть отрицательным.", nameof(weight));
        if (reps <= 0)
            throw new ArgumentException("Количество повторений должно быть больше 0.", nameof(reps));
        if (rpe.HasValue && !ProgramExerciseValidator.IsValidRpeValue(rpe.Value))
            throw new ArgumentException("RPE должен быть от 5.5 до 10 с шагом 0.5.", nameof(rpe));

        var db = await _database.GetConnectionAsync();

        var maxNumber = await db.ExecuteScalarAsync<int>(
            "SELECT IFNULL(MAX(SetNumber), 0) FROM SetEntry WHERE WorkoutExerciseId = ?",
            workoutExerciseId);

        var set = new SetEntry
        {
            WorkoutExerciseId = workoutExerciseId,
            SetNumber = maxNumber + 1,
            Weight = weight,
            Reps = reps,
            Rpe = rpe,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CompletedAt = DateTime.UtcNow
        };

        await db.InsertAsync(set);
        return set;
    }

    public async Task DeleteSetAsync(int setId)
    {
        var db = await _database.GetConnectionAsync();
        var set = await db.FindAsync<SetEntry>(setId);
        if (set == null) return;

        await db.DeleteAsync(set);

        var remaining = await db.Table<SetEntry>()
            .Where(s => s.WorkoutExerciseId == set.WorkoutExerciseId)
            .OrderBy(s => s.SetNumber)
            .ToListAsync();

        for (int i = 0; i < remaining.Count; i++)
        {
            var expected = i + 1;
            if (remaining[i].SetNumber != expected)
            {
                remaining[i].SetNumber = expected;
                await db.UpdateAsync(remaining[i]);
            }
        }
    }

    public async Task FinishSessionAsync(int sessionId, string? comment = null)
    {
        var db = await _database.GetConnectionAsync();
        var session = await db.FindAsync<WorkoutSession>(sessionId)
            ?? throw new ArgumentException($"Сессия с ID {sessionId} не найдена.");

        session.FinishedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(comment))
            session.Comment = comment.Trim();

        await db.UpdateAsync(session);
    }

    public async Task DiscardSessionAsync(int sessionId)
    {
        var db = await _database.GetConnectionAsync();
        var session = await db.FindAsync<WorkoutSession>(sessionId);
        if (session == null) return;

        var workoutExercises = await db.Table<WorkoutExercise>()
            .Where(we => we.WorkoutSessionId == sessionId)
            .ToListAsync();

        foreach (var we in workoutExercises)
        {
            await db.Table<SetEntry>().DeleteAsync(s => s.WorkoutExerciseId == we.Id);
            await db.DeleteAsync(we);
        }

        await db.DeleteAsync(session);
    }

    public async Task<IReadOnlyList<PastWorkoutSummary>> GetPastSessionsAsync()
    {
        var db = await _database.GetConnectionAsync();

        var sessions = await db.Table<WorkoutSession>()
            .Where(s => s.FinishedAt != null)
            .OrderByDescending(s => s.FinishedAt)
            .ToListAsync();

        var list = new List<PastWorkoutSummary>();

        foreach (var s in sessions)
        {
            var exerciseCount = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM WorkoutExercise WHERE WorkoutSessionId = ?",
                s.Id);

            var setsCount = await db.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*) FROM SetEntry se
                INNER JOIN WorkoutExercise we ON we.Id = se.WorkoutExerciseId
                WHERE we.WorkoutSessionId = ?
                """,
                s.Id);

            list.Add(new PastWorkoutSummary
            {
                Id = s.Id,
                Title = s.Title,
                StartedAt = s.StartedAt,
                FinishedAt = s.FinishedAt!.Value,
                ExerciseCount = exerciseCount,
                CompletedSetsCount = setsCount
            });
        }

        return list;
    }

    private sealed class WorkoutExerciseRow
    {
        public int Id { get; set; }
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int ExerciseType { get; set; }
        public int Order { get; set; }
        public int? PlannedSets { get; set; }
        public int? PlannedReps { get; set; }
        public int? PlannedRepsMax { get; set; }
        public double? PlannedWeight { get; set; }
        public double? PlannedRpe { get; set; }
        public double? PlannedRpeMax { get; set; }
        public string? Notes { get; set; }
    }
}
