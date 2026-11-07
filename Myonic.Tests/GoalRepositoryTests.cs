using Myonic.Core.Models;
using Myonic.Data;
using Myonic.Data.Repositories;

namespace Myonic.Tests;

public class GoalRepositoryTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-goal-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private GoalRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _repo = new GoalRepository(_db);
        await _db.GetConnectionAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    [Fact]
    public async Task AddAsync_InsertsAndRetrieves()
    {
        var goal = new Goal
        {
            Title = "Жим лёжа 120 кг",
            Type = GoalType.ExerciseWeight,
            StartValue = 100,
            TargetValue = 120,
            CurrentValue = 100
        };

        var id = await _repo.AddAsync(goal);
        Assert.True(id > 0);

        var retrieved = await _repo.GetByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.Equal("Жим лёжа 120 кг", retrieved.Title);
        Assert.Equal(100, retrieved.StartValue);
        Assert.Equal(120, retrieved.TargetValue);
        Assert.Equal(GoalStatus.Active, retrieved.Status);
    }

    [Fact]
    public void ProgressFraction_IncreasingGoal_CalculatesCorrectly()
    {
        var goal = new Goal
        {
            StartValue = 100,
            TargetValue = 120,
            CurrentValue = 110
        };

        Assert.Equal(0.5, goal.ProgressFraction);
        Assert.Equal(50, goal.ProgressPercent);
        Assert.Equal(10.0, goal.RemainingValue);
        Assert.False(goal.IsTargetMet);

        goal.CurrentValue = 120;
        Assert.Equal(1.0, goal.ProgressFraction);
        Assert.Equal(100, goal.ProgressPercent);
        Assert.Equal(0.0, goal.RemainingValue);
        Assert.True(goal.IsTargetMet);
    }

    [Fact]
    public void ProgressFraction_DecreasingGoal_CalculatesCorrectly()
    {
        var goal = new Goal
        {
            StartValue = 85,
            TargetValue = 75,
            CurrentValue = 80
        };

        // (85 - 80) / (85 - 75) = 5 / 10 = 0.5
        Assert.Equal(0.5, goal.ProgressFraction);
        Assert.Equal(50, goal.ProgressPercent);
        Assert.Equal(5.0, goal.RemainingValue);
        Assert.False(goal.IsTargetMet);

        goal.CurrentValue = 75;
        Assert.Equal(1.0, goal.ProgressFraction);
        Assert.Equal(100, goal.ProgressPercent);
        Assert.Equal(0.0, goal.RemainingValue);
        Assert.True(goal.IsTargetMet);
    }

    [Fact]
    public async Task SyncCurrentValues_BodyWeight_UpdatesFromMeasurements()
    {
        var bodyRepo = new BodyMeasurementRepository(_db);
        await bodyRepo.AddAsync(new BodyMeasurement { Date = DateTime.Today.AddDays(-5), WeightKg = 84.0 });

        var goal = new Goal
        {
            Title = "Сбросить вес до 78 кг",
            Type = GoalType.BodyWeight,
            StartValue = 85.0,
            TargetValue = 78.0,
            CurrentValue = 85.0
        };
        await _repo.AddAsync(goal);

        // CurrentValue automatically updated from the measurement
        var active = await _repo.GetActiveGoalsAsync();
        Assert.Single(active);
        Assert.Equal(84.0, active[0].CurrentValue);

        // Now add a newer measurement
        await bodyRepo.AddAsync(new BodyMeasurement { Date = DateTime.Today, WeightKg = 82.5 });
        await _repo.SyncCurrentValuesAsync();

        var updated = await _repo.GetByIdAsync(goal.Id);
        Assert.NotNull(updated);
        Assert.Equal(82.5, updated.CurrentValue);
    }

    [Fact]
    public async Task SyncCurrentValues_ExerciseWeight_UpdatesFromPR()
    {
        var conn = await _db.GetConnectionAsync();
        var ex = new Exercise { Name = "Приседания со штангой", Type = ExerciseType.Compound };
        await conn.InsertAsync(ex);

        await conn.InsertAsync(new PersonalRecord
        {
            ExerciseId = ex.Id,
            Type = RecordType.MaxWeight,
            Weight = 140.0,
            AchievedAt = DateTime.UtcNow
        });

        var goal = new Goal
        {
            Title = "Присед 160 кг",
            Type = GoalType.ExerciseWeight,
            ExerciseId = ex.Id,
            ExerciseName = ex.Name,
            StartValue = 130.0,
            TargetValue = 160.0,
            CurrentValue = 130.0
        };
        await _repo.AddAsync(goal);

        var active = await _repo.GetActiveGoalsAsync();
        Assert.Single(active);
        Assert.Equal(140.0, active[0].CurrentValue);
    }

    [Fact]
    public async Task SyncCurrentValues_TargetReached_MarksAchieved()
    {
        var bodyRepo = new BodyMeasurementRepository(_db);
        await bodyRepo.AddAsync(new BodyMeasurement { Date = DateTime.Today, WeightKg = 75.0 });

        var goal = new Goal
        {
            Title = "Похудеть до 75 кг",
            Type = GoalType.BodyWeight,
            StartValue = 80.0,
            TargetValue = 75.0,
            CurrentValue = 80.0
        };
        await _repo.AddAsync(goal);

        var all = await _repo.GetAllGoalsAsync();
        Assert.Single(all);
        Assert.Equal(GoalStatus.Achieved, all[0].Status);
        Assert.NotNull(all[0].AchievedAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesGoal()
    {
        var goal = new Goal
        {
            Title = "Тестовая цель",
            Type = GoalType.BodyWeight,
            StartValue = 80,
            TargetValue = 75,
            CurrentValue = 80
        };
        var id = await _repo.AddAsync(goal);

        await _repo.DeleteAsync(id);

        var retrieved = await _repo.GetByIdAsync(id);
        Assert.Null(retrieved);
    }
}
