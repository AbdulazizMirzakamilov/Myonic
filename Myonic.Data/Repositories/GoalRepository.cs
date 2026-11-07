using Myonic.Core.Models;
using Myonic.Core.Repositories;
using SQLite;

namespace Myonic.Data.Repositories;

public sealed class GoalRepository : IGoalRepository
{
    private readonly MyonicDatabase _database;

    public GoalRepository(MyonicDatabase database) => _database = database;

    public async Task<int> AddAsync(Goal goal)
    {
        goal.CreatedAt = DateTime.UtcNow;
        if (goal.CurrentValue == 0)
        {
            goal.CurrentValue = goal.StartValue;
        }

        var db = await _database.GetConnectionAsync();
        await db.InsertAsync(goal);
        await SyncGoalAsync(db, goal);
        return goal.Id;
    }

    public async Task UpdateAsync(Goal goal)
    {
        var db = await _database.GetConnectionAsync();
        await db.UpdateAsync(goal);
    }

    public async Task DeleteAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        await db.DeleteAsync<Goal>(id);
    }

    public async Task<Goal?> GetByIdAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        return await db.FindAsync<Goal>(id);
    }

    public async Task<IReadOnlyList<Goal>> GetActiveGoalsAsync()
    {
        await SyncCurrentValuesAsync();
        var db = await _database.GetConnectionAsync();
        return await db.Table<Goal>()
            .Where(g => g.Status == GoalStatus.Active)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Goal>> GetAllGoalsAsync()
    {
        await SyncCurrentValuesAsync();
        var db = await _database.GetConnectionAsync();
        return await db.Table<Goal>()
            .OrderBy(g => g.Status)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task SyncCurrentValuesAsync()
    {
        var db = await _database.GetConnectionAsync();
        var activeGoals = await db.Table<Goal>()
            .Where(g => g.Status == GoalStatus.Active)
            .ToListAsync();

        foreach (var goal in activeGoals)
        {
            await SyncGoalAsync(db, goal);
        }
    }

    private static async Task SyncGoalAsync(SQLiteAsyncConnection db, Goal goal)
    {
        bool changed = false;

        switch (goal.Type)
        {
            case GoalType.ExerciseWeight:
                if (goal.ExerciseId.HasValue && goal.ExerciseId.Value > 0)
                {
                    var prList = await db.Table<PersonalRecord>()
                        .Where(p => p.ExerciseId == goal.ExerciseId.Value && p.Type == RecordType.MaxWeight)
                        .OrderByDescending(p => p.Weight)
                        .ToListAsync();

                    if (prList.Count > 0 && prList[0].Weight > 0)
                    {
                        var maxWeight = prList[0].Weight;
                        if (Math.Abs(goal.CurrentValue - maxWeight) > 0.01)
                        {
                            goal.CurrentValue = maxWeight;
                            changed = true;
                        }
                    }
                }
                break;

            case GoalType.BodyWeight:
                var latestWeight = await db.Table<BodyMeasurement>()
                    .Where(m => m.WeightKg > 0)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefaultAsync();

                if (latestWeight != null && Math.Abs(goal.CurrentValue - latestWeight.WeightKg) > 0.01)
                {
                    goal.CurrentValue = latestWeight.WeightKg;
                    changed = true;
                }
                break;

            case GoalType.BodyMeasurement:
                var measurements = await db.Table<BodyMeasurement>()
                    .OrderByDescending(m => m.Date)
                    .Take(20)
                    .ToListAsync();

                double? val = null;
                var part = goal.BodyPart?.Trim().ToLowerInvariant() ?? "";

                foreach (var m in measurements)
                {
                    if (part.Contains("бицепс") && m.BicepsCm.HasValue) { val = m.BicepsCm; break; }
                    if (part.Contains("тали") && m.WaistCm.HasValue) { val = m.WaistCm; break; }
                    if (part.Contains("груд") && m.ChestCm.HasValue) { val = m.ChestCm; break; }
                    if (part.Contains("бедр") && m.ThighCm.HasValue) { val = m.ThighCm; break; }
                    if (part.Contains("бёдр") && m.HipsCm.HasValue) { val = m.HipsCm; break; }
                    if (part.Contains("икр") && m.CalfCm.HasValue) { val = m.CalfCm; break; }
                    if (part.Contains("ше") && m.NeckCm.HasValue) { val = m.NeckCm; break; }
                    if (part.Contains("жир") && m.BodyFatPercentage.HasValue) { val = m.BodyFatPercentage; break; }
                }

                if (val.HasValue && Math.Abs(goal.CurrentValue - val.Value) > 0.01)
                {
                    goal.CurrentValue = val.Value;
                    changed = true;
                }
                break;

            case GoalType.WorkoutsFrequency:
                var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var sessionsThisMonth = await db.Table<WorkoutSession>()
                    .Where(s => s.FinishedAt >= monthStart && s.FinishedAt != null)
                    .CountAsync();

                if (Math.Abs(goal.CurrentValue - sessionsThisMonth) > 0.01)
                {
                    goal.CurrentValue = sessionsThisMonth;
                    changed = true;
                }
                break;
        }

        if (goal.IsTargetMet && goal.Status == GoalStatus.Active)
        {
            goal.Status = GoalStatus.Achieved;
            goal.AchievedAt = DateTime.UtcNow;
            changed = true;
        }

        if (changed)
        {
            await db.UpdateAsync(goal);
        }
    }
}
