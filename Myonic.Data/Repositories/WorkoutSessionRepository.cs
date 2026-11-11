using Myonic.Core.Calculators;
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

    public async Task<int> StartSessionFromProgramWorkoutAsync(int programWorkoutId, int? scheduledWorkoutId = null)
    {
        var db = await _database.GetConnectionAsync();

        var programWorkout = await db.FindAsync<ProgramWorkout>(programWorkoutId)
            ?? throw new ArgumentException($"Тренировка с ID {programWorkoutId} не найдена.");

        // Закрываем любые предыдущие незавершённые сессии
        var oldActive = await db.Table<WorkoutSession>()
            .Where(s => s.FinishedAt == null)
            .ToListAsync();
        foreach (var o in oldActive)
        {
            o.FinishedAt = DateTime.UtcNow;
            await db.UpdateAsync(o);
        }

        var session = new WorkoutSession
        {
            Title = programWorkout.Name,
            ScheduledWorkoutId = scheduledWorkoutId,
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

    public async Task<int> StartEmptySessionAsync(string title)
    {
        var db = await _database.GetConnectionAsync();

        var oldActive = await db.Table<WorkoutSession>()
            .Where(s => s.FinishedAt == null)
            .ToListAsync();
        foreach (var o in oldActive)
        {
            o.FinishedAt = DateTime.UtcNow;
            await db.UpdateAsync(o);
        }

        var session = new WorkoutSession
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Свободная тренировка" : title.Trim(),
            StartedAt = DateTime.UtcNow
        };
        await db.InsertAsync(session);
        return session.Id;
    }

    public async Task<SessionExerciseDetail> AddExerciseToSessionAsync(int sessionId, int exerciseId)
    {
        var db = await _database.GetConnectionAsync();
        var exercise = await db.FindAsync<Exercise>(exerciseId)
            ?? throw new ArgumentException($"Упражнение с ID {exerciseId} не найдено.");

        var maxOrder = await db.ExecuteScalarAsync<int>(
            "SELECT IFNULL(MAX(\"Order\"), 0) FROM WorkoutExercise WHERE WorkoutSessionId = ?",
            sessionId);

        var we = new WorkoutExercise
        {
            WorkoutSessionId = sessionId,
            ExerciseId = exerciseId,
            Order = maxOrder + 1
        };
        await db.InsertAsync(we);

        return new SessionExerciseDetail
        {
            WorkoutExerciseId = we.Id,
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            ExerciseType = exercise.Type,
            Order = we.Order,
            Sets = []
        };
    }

    public async Task DeleteExerciseFromSessionAsync(int workoutExerciseId)
    {
        var db = await _database.GetConnectionAsync();
        var we = await db.FindAsync<WorkoutExercise>(workoutExerciseId);
        if (we == null) return;

        // Удаляем связанные личные рекорды, созданные сетами этого упражнения
        await db.ExecuteAsync("""
            DELETE FROM PersonalRecord
            WHERE SetEntryId IN (SELECT Id FROM SetEntry WHERE WorkoutExerciseId = ?)
            """, workoutExerciseId);

        await db.Table<SetEntry>().DeleteAsync(s => s.WorkoutExerciseId == workoutExerciseId);
        await db.DeleteAsync(we);
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

        // Удаляем личный рекорд, если он был зафиксирован этим подходом
        await db.ExecuteAsync("DELETE FROM PersonalRecord WHERE SetEntryId = ?", setId);

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

        var finishTime = DateTime.UtcNow;
        session.FinishedAt = finishTime;
        if (!string.IsNullOrWhiteSpace(comment))
            session.Comment = comment.Trim();

        await db.UpdateAsync(session);

        if (session.ScheduledWorkoutId.HasValue)
        {
            var sw = await db.FindAsync<ScheduledWorkout>(session.ScheduledWorkoutId.Value);
            if (sw != null)
            {
                sw.Status = ScheduledWorkoutStatus.Completed;
                await db.UpdateAsync(sw);
            }
        }

        // Гарантируем, что в базе не останется других незакрытых «зависших» сессий
        var dangling = await db.Table<WorkoutSession>()
            .Where(s => s.FinishedAt == null)
            .ToListAsync();
        foreach (var d in dangling)
        {
            d.FinishedAt = finishTime;
            await db.UpdateAsync(d);
        }
    }

    public async Task DiscardSessionAsync(int sessionId)
    {
        var db = await _database.GetConnectionAsync();
        var session = await db.FindAsync<WorkoutSession>(sessionId);
        if (session == null) return;

        // Удаляем все личные рекорды, зафиксированные в этой отменяемой сессии
        await db.ExecuteAsync("""
            DELETE FROM PersonalRecord
            WHERE SetEntryId IN (
                SELECT se.Id FROM SetEntry se
                INNER JOIN WorkoutExercise we ON we.Id = se.WorkoutExerciseId
                WHERE we.WorkoutSessionId = ?
            )
            """, sessionId);

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

    public async Task<int> GetCompletedSessionsCountAsync()
    {
        var db = await _database.GetConnectionAsync();
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutSession WHERE FinishedAt IS NOT NULL");
    }

    public async Task<IReadOnlyList<PastWorkoutSummary>> GetPastSessionsAsync()
    {
        var db = await _database.GetConnectionAsync();

        return await db.QueryAsync<PastWorkoutSummary>(
            """
            SELECT
                s.Id,
                s.Title,
                s.StartedAt,
                s.FinishedAt,
                COUNT(DISTINCT we.Id) AS ExerciseCount,
                COUNT(se.Id) AS CompletedSetsCount
            FROM WorkoutSession s
            LEFT JOIN WorkoutExercise we ON we.WorkoutSessionId = s.Id
            LEFT JOIN SetEntry se ON se.WorkoutExerciseId = we.Id
            WHERE s.FinishedAt IS NOT NULL
            GROUP BY s.Id
            ORDER BY s.FinishedAt DESC
            """);
    }

    public async Task<IReadOnlyList<DateTime>> GetCompletedSessionDatesAsync(DateTime fromDate, DateTime toDate)
    {
        var db = await _database.GetConnectionAsync();
        bool isUtc = fromDate.Kind == DateTimeKind.Utc;

        var fromUtc = isUtc ? fromDate : DateTime.SpecifyKind(fromDate, DateTimeKind.Local).ToUniversalTime();
        var toUtc = isUtc ? toDate : DateTime.SpecifyKind(toDate, DateTimeKind.Local).ToUniversalTime();

        var sessions = await db.Table<WorkoutSession>()
            .Where(s => s.FinishedAt != null && s.FinishedAt >= fromUtc && s.FinishedAt <= toUtc)
            .ToListAsync();

        return sessions
            .Select(s =>
            {
                if (isUtc)
                {
                    return s.FinishedAt!.Value.Date;
                }
                var utcTime = DateTime.SpecifyKind(s.FinishedAt!.Value, DateTimeKind.Utc);
                return utcTime.ToLocalTime().Date;
            })
            .Distinct()
            .OrderBy(d => d)
            .ToList();
    }

    public async Task<IReadOnlyList<WorkoutSessionDetail>> GetSessionsForDateAsync(DateTime date)
    {
        var db = await _database.GetConnectionAsync();
        bool isUtc = date.Kind == DateTimeKind.Utc;

        DateTime startUtc;
        DateTime endUtc;

        if (isUtc)
        {
            startUtc = date.Date;
            endUtc = startUtc.AddDays(1);
        }
        else
        {
            var startLocal = date.Date;
            var endLocal = startLocal.AddDays(1);
            startUtc = DateTime.SpecifyKind(startLocal, DateTimeKind.Local).ToUniversalTime();
            endUtc = DateTime.SpecifyKind(endLocal, DateTimeKind.Local).ToUniversalTime();
        }

        var sessions = await db.Table<WorkoutSession>()
            .Where(s => s.FinishedAt != null && s.FinishedAt >= startUtc && s.FinishedAt < endUtc)
            .OrderByDescending(s => s.FinishedAt)
            .ToListAsync();

        var result = new List<WorkoutSessionDetail>();
        foreach (var s in sessions)
        {
            var detail = await GetSessionDetailAsync(s.Id);
            if (detail != null)
                result.Add(detail);
        }

        return result;
    }

    public async Task<ExerciseHistorySummary> GetExerciseHistoryAsync(int exerciseId)
    {
        var db = await _database.GetConnectionAsync();
        var exercise = await db.Table<Exercise>().Where(e => e.Id == exerciseId).FirstOrDefaultAsync();
        if (exercise == null)
            throw new InvalidOperationException($"Упражнение с ID {exerciseId} не найдено.");

        var rows = await db.QueryAsync<ExerciseSessionHistoryRow>(
            @"SELECT we.Id AS WorkoutExerciseId,
                     ws.Id AS SessionId,
                     ws.Title AS WorkoutTitle,
                     ws.StartedAt AS SessionStartedAt
              FROM WorkoutExercise we
              JOIN WorkoutSession ws ON we.WorkoutSessionId = ws.Id
              WHERE we.ExerciseId = ?
              ORDER BY ws.StartedAt DESC",
            exerciseId);

        var sessions = new List<ExerciseHistoryItem>();
        double allTimeMaxWeight = 0;
        double allTimeBest1Rm = 0;
        int totalSets = 0;

        foreach (var r in rows)
        {
            var sets = await db.Table<SetEntry>()
                .Where(s => s.WorkoutExerciseId == r.WorkoutExerciseId)
                .OrderBy(s => s.SetNumber)
                .ToListAsync();

            if (sets.Count == 0) continue;

            totalSets += sets.Count;
            var setItems = new List<ExerciseHistorySetItem>();
            double maxWeightInSession = 0;
            double best1RmInSession = 0;
            double sessionVolume = 0;

            foreach (var s in sets)
            {
                // Для «Большой тройки» (жим, присед, тяга) считаем средний 1ПМ по всем формулам; для остальных – только фактические 1-повторные подходы
                double est1Rm = OneRepMaxCalculator.IsBigThree(exercise.Name)
                    ? OneRepMaxCalculator.CalculateBigThreeAverage1Rm(s.Weight, s.Reps)
                    : (s.Reps == 1 ? s.Weight : 0);

                if (s.Weight > maxWeightInSession) maxWeightInSession = s.Weight;
                if (est1Rm > best1RmInSession) best1RmInSession = est1Rm;
                sessionVolume += s.Weight * s.Reps;

                setItems.Add(new ExerciseHistorySetItem
                {
                    SetNumber = s.SetNumber,
                    Weight = s.Weight,
                    Reps = s.Reps,
                    Rpe = s.Rpe,
                    Estimated1Rm = est1Rm
                });
            }

            if (maxWeightInSession > allTimeMaxWeight) allTimeMaxWeight = maxWeightInSession;
            if (best1RmInSession > allTimeBest1Rm) allTimeBest1Rm = best1RmInSession;

            sessions.Add(new ExerciseHistoryItem
            {
                SessionId = r.SessionId,
                WorkoutTitle = string.IsNullOrWhiteSpace(r.WorkoutTitle) ? "Тренировка" : r.WorkoutTitle,
                Date = r.SessionStartedAt,
                MaxWeight = maxWeightInSession,
                Best1Rm = best1RmInSession,
                TotalVolume = sessionVolume,
                Sets = setItems
            });
        }

        return new ExerciseHistorySummary
        {
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            ExerciseType = exercise.Type,
            Description = exercise.Description,
            AllTimeMaxWeight = allTimeMaxWeight,
            AllTimeBest1Rm = allTimeBest1Rm,
            TotalSessionsCount = sessions.Count,
            TotalSetsCount = totalSets,
            Sessions = sessions
        };
    }

    public async Task<AnalyticsSummary> GetAnalyticsSummaryAsync(AnalyticsPeriod period = AnalyticsPeriod.Days30)
    {
        var db = await _database.GetConnectionAsync();
        DateTime nowUtc = DateTime.UtcNow;
        DateTime fromDateUtc = period switch
        {
            AnalyticsPeriod.Days30 => nowUtc.AddDays(-30),
            AnalyticsPeriod.Days90 => nowUtc.AddDays(-90),
            AnalyticsPeriod.Days180 => nowUtc.AddDays(-180),
            _ => DateTime.MinValue
        };

        var sessions = await db.Table<WorkoutSession>()
            .Where(s => s.FinishedAt != null && s.FinishedAt >= fromDateUtc)
            .OrderBy(s => s.StartedAt)
            .ToListAsync();

        var summary = new AnalyticsSummary { Period = period };
        if (sessions.Count == 0)
        {
            return summary;
        }

        var setRows = await db.QueryAsync<AnalyticsSetRow>(
            @"SELECT
                ws.Id AS SessionId,
                ws.StartedAt,
                ws.FinishedAt,
                e.Id AS ExerciseId,
                e.Name AS ExerciseName,
                e.Type AS ExerciseType,
                se.SetNumber,
                se.Weight,
                se.Reps
              FROM SetEntry se
              JOIN WorkoutExercise we ON se.WorkoutExerciseId = we.Id
              JOIN WorkoutSession ws ON we.WorkoutSessionId = ws.Id
              JOIN Exercise e ON we.ExerciseId = e.Id
              WHERE ws.FinishedAt IS NOT NULL AND ws.FinishedAt >= ?
              ORDER BY ws.StartedAt ASC, we.""Order"" ASC, se.SetNumber ASC",
            fromDateUtc);

        summary.TotalWorkouts = sessions.Count;
        summary.TotalSets = setRows.Count;
        summary.TotalReps = setRows.Sum(s => s.Reps);
        summary.TotalVolumeKg = setRows.Sum(s => s.Weight * s.Reps);

        var durations = sessions
            .Select(s => (s.FinishedAt!.Value - s.StartedAt).TotalMinutes)
            .Where(m => m > 0 && m < 600)
            .ToList();

        if (durations.Count > 0)
        {
            summary.AverageDuration = TimeSpan.FromMinutes(durations.Average());
        }

        double spanDays = period == AnalyticsPeriod.All
            ? Math.Max(7, (nowUtc - sessions.Min(s => s.StartedAt)).TotalDays)
            : (double)(int)period;

        summary.WorkoutsPerWeek = Math.Round((summary.TotalWorkouts / spanDays) * 7.0, 1);

        // PRs count
        if (fromDateUtc == DateTime.MinValue)
        {
            summary.PersonalRecordsCount = await db.Table<PersonalRecord>().CountAsync();
        }
        else
        {
            summary.PersonalRecordsCount = await db.Table<PersonalRecord>()
                .Where(pr => pr.AchievedAt >= fromDateUtc)
                .CountAsync();
        }

        summary.CompoundSetsCount = setRows.Count(s => (ExerciseType)s.ExerciseType == ExerciseType.Compound);
        summary.IsolationSetsCount = setRows.Count(s => (ExerciseType)s.ExerciseType == ExerciseType.Isolation);

        // Weekly Volume Buckets
        var culture = new System.Globalization.CultureInfo("ru-RU");
        DateTime earliestLocal = (period == AnalyticsPeriod.All ? sessions.Min(s => s.StartedAt) : fromDateUtc).ToLocalTime().Date;
        DateTime latestLocal = nowUtc.ToLocalTime().Date;

        int earliestDiff = (7 + (earliestLocal.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime weekIter = earliestLocal.AddDays(-earliestDiff);

        int latestDiff = (7 + (latestLocal.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime currentWeekStart = latestLocal.AddDays(-latestDiff);

        var setsBySession = setRows.GroupBy(s => s.SessionId).ToDictionary(g => g.Key, g => g.ToList());
        var sessionsByWeek = new Dictionary<DateTime, List<WorkoutSession>>();

        foreach (var sess in sessions)
        {
            var lDate = sess.StartedAt.ToLocalTime().Date;
            int d = (7 + (lDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            var wStart = lDate.AddDays(-d);
            if (!sessionsByWeek.TryGetValue(wStart, out var list))
            {
                list = [];
                sessionsByWeek[wStart] = list;
            }
            list.Add(sess);
        }

        while (weekIter <= currentWeekStart)
        {
            double weekVol = 0;
            int count = 0;
            if (sessionsByWeek.TryGetValue(weekIter, out var wSessions))
            {
                count = wSessions.Count;
                foreach (var ws in wSessions)
                {
                    if (setsBySession.TryGetValue(ws.Id, out var setsInSession))
                    {
                        weekVol += setsInSession.Sum(s => s.Weight * s.Reps);
                    }
                }
            }

            summary.WeeklyPoints.Add(new WeeklyVolumePoint(
                weekIter,
                weekIter.ToString("d MMM", culture),
                weekVol,
                count));

            weekIter = weekIter.AddDays(7);
        }

        // Muscle Distribution
        var muscleGroups = new Dictionary<string, (int sets, double volume)>();
        foreach (var s in setRows)
        {
            string group = ClassifyMuscleGroup(s.ExerciseName);
            if (!muscleGroups.TryGetValue(group, out var current))
            {
                current = (0, 0);
            }
            muscleGroups[group] = (current.sets + 1, current.volume + (s.Weight * s.Reps));
        }

        double totalVol = summary.TotalVolumeKg > 0 ? summary.TotalVolumeKg : 1;
        summary.MuscleDistribution = muscleGroups
            .Select(kv => new MuscleGroupDistributionItem(
                kv.Key,
                kv.Value.sets,
                kv.Value.volume,
                Math.Round((kv.Value.volume / totalVol) * 100.0, 1)))
            .OrderByDescending(m => m.SetsCount)
            .ToList();

        return summary;
    }

    private static string ClassifyMuscleGroup(string name)
    {
        var lower = name.ToLowerInvariant();
        if ((lower.Contains("жим") && (lower.Contains("лёжа") || lower.Contains("лежа") || lower.Contains("скамье") || lower.Contains("брусьях"))) ||
            lower.Contains("разведение гантелей лёжа") || lower.Contains("кроссовер") || lower.Contains("груд"))
            return "Грудь";

        if (lower.Contains("тяга") || lower.Contains("подтягивания") || lower.Contains("спин"))
            return "Спина";

        if (lower.Contains("присед") || lower.Contains("ногами") || lower.Contains("выпады") || lower.Contains("ноги") ||
            lower.Contains("голень") || lower.Contains("носок") || lower.Contains("квадрицепс") || lower.Contains("бедр"))
            return "Ноги";

        if (lower.Contains("жим штанги стоя") || lower.Contains("жим стоя") || lower.Contains("армейский") ||
            lower.Contains("махи") || lower.Contains("плеч") || lower.Contains("дельта"))
            return "Плечи";

        if (lower.Contains("бицепс") || lower.Contains("трицепс") || lower.Contains("французский") ||
            lower.Contains("молотков") || lower.Contains("сгибание рук") || lower.Contains("разгибание рук"))
            return "Руки";

        if (lower.Contains("пресс") || lower.Contains("скручиван") || lower.Contains("планк") || lower.Contains("кор"))
            return "Кор";

        return "Другое";
    }

    private sealed class AnalyticsSetRow
    {
        public int SessionId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int ExerciseType { get; set; }
        public int SetNumber { get; set; }
        public double Weight { get; set; }
        public int Reps { get; set; }
    }

    private sealed class ExerciseSessionHistoryRow
    {
        public int WorkoutExerciseId { get; set; }
        public int SessionId { get; set; }
        public string WorkoutTitle { get; set; } = string.Empty;
        public DateTime SessionStartedAt { get; set; }
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
