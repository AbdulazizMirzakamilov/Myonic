using Myonic.Core.Calculators;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Data.Repositories;

public sealed class PersonalRecordRepository : IPersonalRecordRepository
{
    private readonly MyonicDatabase _database;

    public PersonalRecordRepository(MyonicDatabase database) => _database = database;

    public async Task<IReadOnlyList<PersonalRecordItem>> GetAllAsync()
    {
        var db = await _database.GetConnectionAsync();
        var rows = await db.QueryAsync<PersonalRecordRow>(
            @"SELECT pr.Id, pr.ExerciseId, e.Name AS ExerciseName, pr.Type, pr.Weight, pr.Reps, pr.AchievedAt, pr.SetEntryId
              FROM PersonalRecord pr
              JOIN Exercise e ON pr.ExerciseId = e.Id
              ORDER BY pr.AchievedAt DESC");

        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PersonalRecordItem>> GetByExerciseAsync(int exerciseId)
    {
        var db = await _database.GetConnectionAsync();
        var rows = await db.QueryAsync<PersonalRecordRow>(
            @"SELECT pr.Id, pr.ExerciseId, e.Name AS ExerciseName, pr.Type, pr.Weight, pr.Reps, pr.AchievedAt, pr.SetEntryId
              FROM PersonalRecord pr
              JOIN Exercise e ON pr.ExerciseId = e.Id
              WHERE pr.ExerciseId = ?
              ORDER BY pr.AchievedAt DESC",
            exerciseId);

        return rows.Select(Map).ToList();
    }

    public async Task<PersonalRecordItem?> GetBestRecordAsync(int exerciseId, RecordType type)
    {
        var db = await _database.GetConnectionAsync();
        var rows = await db.QueryAsync<PersonalRecordRow>(
            @"SELECT pr.Id, pr.ExerciseId, e.Name AS ExerciseName, pr.Type, pr.Weight, pr.Reps, pr.AchievedAt, pr.SetEntryId
              FROM PersonalRecord pr
              JOIN Exercise e ON pr.ExerciseId = e.Id
              WHERE pr.ExerciseId = ? AND pr.Type = ?
              ORDER BY pr.Weight DESC, pr.Reps DESC, pr.AchievedAt DESC
              LIMIT 1",
            exerciseId,
            (int)type);

        var first = rows.FirstOrDefault();
        return first == null ? null : Map(first);
    }

    public async Task<int> AddManualRecordAsync(int exerciseId, RecordType type, double weight, int reps, DateTime achievedAt)
    {
        if (weight <= 0)
            throw new ArgumentException("Вес должен быть больше 0.", nameof(weight));
        if (reps <= 0)
            throw new ArgumentException("Количество повторений должно быть больше 0.", nameof(reps));

        var db = await _database.GetConnectionAsync();
        var ex = await db.Table<Exercise>().Where(e => e.Id == exerciseId).FirstOrDefaultAsync();
        if (ex == null)
            throw new InvalidOperationException($"Упражнение с ID {exerciseId} не найдено.");

        var record = new PersonalRecord
        {
            ExerciseId = exerciseId,
            Type = type,
            Weight = weight,
            Reps = reps,
            AchievedAt = achievedAt,
            SetEntryId = null
        };

        await db.InsertAsync(record);
        return record.Id;
    }

    public async Task DeleteRecordAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        await db.DeleteAsync<PersonalRecord>(id);
    }

    public async Task<IReadOnlyList<PersonalRecordItem>> CheckAndRecordNewPrsAsync(
        int workoutExerciseId,
        int setEntryId,
        double weight,
        int reps)
    {
        if (weight <= 0 || reps <= 0) return [];

        var db = await _database.GetConnectionAsync();
        var we = await db.Table<WorkoutExercise>().Where(w => w.Id == workoutExerciseId).FirstOrDefaultAsync();
        if (we == null) return [];

        var exercise = await db.Table<Exercise>().Where(e => e.Id == we.ExerciseId).FirstOrDefaultAsync();
        if (exercise == null) return [];

        var newPrs = new List<PersonalRecordItem>();
        var now = DateTime.UtcNow;

        // 1. Check MaxWeight
        var currentMaxWeight = await db.ExecuteScalarAsync<double?>(
            "SELECT MAX(Weight) FROM PersonalRecord WHERE ExerciseId = ? AND Type = ?",
            we.ExerciseId,
            (int)RecordType.MaxWeight);

        if (currentMaxWeight == null || weight > currentMaxWeight.Value)
        {
            var pr = new PersonalRecord
            {
                ExerciseId = we.ExerciseId,
                Type = RecordType.MaxWeight,
                Weight = weight,
                Reps = reps,
                AchievedAt = now,
                SetEntryId = setEntryId
            };
            await db.InsertAsync(pr);
            newPrs.Add(new PersonalRecordItem
            {
                Id = pr.Id,
                ExerciseId = pr.ExerciseId,
                ExerciseName = exercise.Name,
                Type = pr.Type,
                Weight = pr.Weight,
                Reps = pr.Reps,
                AchievedAt = pr.AchievedAt,
                SetEntryId = pr.SetEntryId
            });
        }

        // 2. Check 1RM (OneRepMax)
        double est1Rm = reps == 1
            ? weight
            : Math.Round(OneRepMaxCalculator.Calculate(OneRepMaxFormula.Epley, weight, Math.Min(reps, OneRepMaxCalculator.MaxReps)), 1);

        var current1Rm = await db.ExecuteScalarAsync<double?>(
            "SELECT MAX(Weight) FROM PersonalRecord WHERE ExerciseId = ? AND Type = ?",
            we.ExerciseId,
            (int)RecordType.OneRepMax);

        if (current1Rm == null || est1Rm > current1Rm.Value)
        {
            var pr = new PersonalRecord
            {
                ExerciseId = we.ExerciseId,
                Type = RecordType.OneRepMax,
                Weight = est1Rm,
                Reps = reps,
                AchievedAt = now,
                SetEntryId = setEntryId
            };
            await db.InsertAsync(pr);
            newPrs.Add(new PersonalRecordItem
            {
                Id = pr.Id,
                ExerciseId = pr.ExerciseId,
                ExerciseName = exercise.Name,
                Type = pr.Type,
                Weight = pr.Weight,
                Reps = pr.Reps,
                AchievedAt = pr.AchievedAt,
                SetEntryId = pr.SetEntryId
            });
        }

        // 3. Check 5RM (FiveRepMax) - when reps >= 5
        if (reps >= 5)
        {
            var current5Rm = await db.ExecuteScalarAsync<double?>(
                "SELECT MAX(Weight) FROM PersonalRecord WHERE ExerciseId = ? AND Type = ?",
                we.ExerciseId,
                (int)RecordType.FiveRepMax);

            if (current5Rm == null || weight > current5Rm.Value)
            {
                var pr = new PersonalRecord
                {
                    ExerciseId = we.ExerciseId,
                    Type = RecordType.FiveRepMax,
                    Weight = weight,
                    Reps = reps,
                    AchievedAt = now,
                    SetEntryId = setEntryId
                };
                await db.InsertAsync(pr);
                newPrs.Add(new PersonalRecordItem
                {
                    Id = pr.Id,
                    ExerciseId = pr.ExerciseId,
                    ExerciseName = exercise.Name,
                    Type = pr.Type,
                    Weight = pr.Weight,
                    Reps = pr.Reps,
                    AchievedAt = pr.AchievedAt,
                    SetEntryId = pr.SetEntryId
                });
            }
        }

        return newPrs;
    }

    private static PersonalRecordItem Map(PersonalRecordRow r) => new()
    {
        Id = r.Id,
        ExerciseId = r.ExerciseId,
        ExerciseName = r.ExerciseName,
        Type = (RecordType)r.Type,
        Weight = r.Weight,
        Reps = r.Reps,
        AchievedAt = r.AchievedAt,
        SetEntryId = r.SetEntryId
    };

    private sealed class PersonalRecordRow
    {
        public int Id { get; set; }
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int Type { get; set; }
        public double Weight { get; set; }
        public int Reps { get; set; }
        public DateTime AchievedAt { get; set; }
        public int? SetEntryId { get; set; }
    }
}
