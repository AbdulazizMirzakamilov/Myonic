using Myonic.Core.Models;
using Myonic.Core.Validation;
using Myonic.Data;
using Myonic.Data.Repositories;

namespace Myonic.Tests;

public class ProgramExerciseRepositoryTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private ProgramExerciseRepository _repo = null!;
    private int _programId;
    private int _workoutId;
    private int _exercise1Id;
    private int _exercise2Id;

    public async Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _repo = new ProgramExerciseRepository(_db);

        var conn = await _db.GetConnectionAsync();

        var program = new TrainingProgram { Name = "Test Program" };
        await conn.InsertAsync(program);
        _programId = program.Id;

        var workout = new ProgramWorkout { ProgramId = _programId, Name = "Day A", DefaultDayOfWeek = DayOfWeek.Monday };
        await conn.InsertAsync(workout);
        _workoutId = workout.Id;

        var ex1 = await conn.Table<Exercise>().FirstAsync();
        _exercise1Id = ex1.Id;

        var ex2 = await conn.Table<Exercise>().Skip(1).FirstAsync();
        _exercise2Id = ex2.Id;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    [Fact]
    public async Task AddAsync_AssignsAutoIncrementingOrder()
    {
        var e1 = new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _exercise1Id,
            Sets = 3,
            Reps = 8
        };
        var e2 = new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _exercise2Id,
            Sets = 4,
            Reps = 10
        };

        var id1 = await _repo.AddAsync(e1);
        var id2 = await _repo.AddAsync(e2);

        var loaded1 = await _repo.GetByIdAsync(id1);
        var loaded2 = await _repo.GetByIdAsync(id2);

        Assert.NotNull(loaded1);
        Assert.NotNull(loaded2);
        Assert.Equal(1, loaded1.Order);
        Assert.Equal(2, loaded2.Order);
    }

    [Fact]
    public async Task GetByWorkoutIdAsync_ReturnsDetailsWithExerciseName()
    {
        await _repo.AddAsync(new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _exercise1Id,
            Sets = 4,
            Reps = 8,
            RepsMax = 12,
            Weight = 80,
            Rpe = 7.5,
            RpeMax = 8.5
        });

        var list = await _repo.GetByWorkoutIdAsync(_workoutId);

        Assert.Single(list);
        var item = list[0];
        Assert.False(string.IsNullOrWhiteSpace(item.ExerciseName));
        Assert.Equal(4, item.Sets);
        Assert.Equal(8, item.Reps);
        Assert.Equal(12, item.RepsMax);
        Assert.Equal(80, item.Weight);
        Assert.Equal(7.5, item.Rpe);
        Assert.Equal(8.5, item.RpeMax);
    }

    [Fact]
    public async Task MoveUp_And_MoveDown_SwapsOrders()
    {
        var id1 = await _repo.AddAsync(new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _exercise1Id,
            Sets = 3,
            Reps = 8
        });
        var id2 = await _repo.AddAsync(new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _exercise2Id,
            Sets = 3,
            Reps = 10
        });

        // Move second up
        await _repo.MoveUpAsync(id2);

        var listAfterMoveUp = await _repo.GetByWorkoutIdAsync(_workoutId);
        Assert.Equal(id2, listAfterMoveUp[0].Id);
        Assert.Equal(1, listAfterMoveUp[0].Order);
        Assert.Equal(id1, listAfterMoveUp[1].Id);
        Assert.Equal(2, listAfterMoveUp[1].Order);

        // Move first down
        await _repo.MoveDownAsync(id2);

        var listAfterMoveDown = await _repo.GetByWorkoutIdAsync(_workoutId);
        Assert.Equal(id1, listAfterMoveDown[0].Id);
        Assert.Equal(id2, listAfterMoveDown[1].Id);
    }

    [Fact]
    public async Task DeleteAsync_RecompactsOrders()
    {
        var id1 = await _repo.AddAsync(new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _exercise1Id,
            Sets = 3,
            Reps = 8
        });
        var id2 = await _repo.AddAsync(new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _exercise2Id,
            Sets = 3,
            Reps = 10
        });
        var id3 = await _repo.AddAsync(new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _exercise1Id,
            Sets = 3,
            Reps = 12
        });

        // Delete middle exercise
        await _repo.DeleteAsync(id2);

        var list = await _repo.GetByWorkoutIdAsync(_workoutId);
        Assert.Equal(2, list.Count);
        Assert.Equal(id1, list[0].Id);
        Assert.Equal(1, list[0].Order);
        Assert.Equal(id3, list[1].Id);
        Assert.Equal(2, list[1].Order);
    }

    [Theory]
    [InlineData(5.5, true)]
    [InlineData(6.0, true)]
    [InlineData(7.5, true)]
    [InlineData(8.0, true)]
    [InlineData(9.5, true)]
    [InlineData(10.0, true)]
    [InlineData(5.0, false)]
    [InlineData(10.5, false)]
    [InlineData(7.3, false)]
    [InlineData(8.2, false)]
    public void ProgramExerciseValidator_RpeValues(double rpe, bool isValid)
    {
        Assert.Equal(isValid, ProgramExerciseValidator.IsValidRpeValue(rpe));
    }

    [Fact]
    public void ProgramExerciseValidator_RpeMax_MustBeGreaterThanRpe()
    {
        var ex = new ProgramExercise
        {
            Sets = 3,
            Reps = 8,
            Rpe = 8.0,
            RpeMax = 8.0
        };

        Assert.Throws<ArgumentException>(() => ProgramExerciseValidator.Validate(ex));

        ex.RpeMax = 7.5;
        Assert.Throws<ArgumentException>(() => ProgramExerciseValidator.Validate(ex));

        ex.RpeMax = 9.0;
        ProgramExerciseValidator.Validate(ex); // Passes
    }

    [Fact]
    public void ProgramExerciseValidator_RepsMax_MustBeGreaterThanReps()
    {
        var ex = new ProgramExercise
        {
            Sets = 3,
            Reps = 8,
            RepsMax = 8
        };

        Assert.Throws<ArgumentException>(() => ProgramExerciseValidator.Validate(ex));

        ex.RepsMax = 6;
        Assert.Throws<ArgumentException>(() => ProgramExerciseValidator.Validate(ex));

        ex.RepsMax = 12;
        ProgramExerciseValidator.Validate(ex); // Passes
    }
}
