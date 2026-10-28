using Myonic.Core.Models;
using Myonic.Data;
using Myonic.Data.Repositories;

namespace Myonic.Tests;

public class ProgramExerciseRepositoryTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private ProgramExerciseRepository _repo = null!;
    private TrainingProgramRepository _programs = null!;
    private int _workoutId;
    private int _benchId;
    private int _squatId;

    public async Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _repo = new ProgramExerciseRepository(_db);
        _programs = new TrainingProgramRepository(_db);

        var programId = await _programs.AddProgramAsync(new TrainingProgram { Name = "P" });
        _workoutId = await _programs.AddWorkoutAsync(
            new ProgramWorkout { ProgramId = programId, Name = "Upper" });

        var conn = await _db.GetConnectionAsync();
        _benchId = (await conn.Table<Exercise>().FirstAsync(e => e.Name == "Жим лёжа")).Id;
        _squatId = (await conn.Table<Exercise>().FirstAsync(e => e.Name == "Приседания со штангой")).Id;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    private Task<int> AddAsync(int exerciseId) =>
        _repo.AddAsync(new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = exerciseId,
            Sets = 3,
            Reps = 8
        });

    [Fact]
    public async Task Add_AssignsIncreasingOrder()
    {
        var a = await AddAsync(_benchId);
        var b = await AddAsync(_squatId);

        var list = await _repo.GetByWorkoutAsync(_workoutId);

        Assert.Equal([a, b], list.Select(x => x.Id));
        Assert.Equal([1, 2], list.Select(x => x.Order));
    }

    [Fact]
    public async Task GetByWorkout_ReturnsExerciseNameAndPlanWithRanges()
    {
        await _repo.AddAsync(new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _benchId,
            Sets = 4,
            Reps = 8,
            RepsMax = 12,
            Weight = 80,
            Rpe = 7.5,
            RpeMax = 9.0,
            RestSeconds = 120,
            Notes = "Пауза на груди"
        });

        var item = (await _repo.GetByWorkoutAsync(_workoutId)).Single();

        Assert.Equal("Жим лёжа", item.ExerciseName);
        Assert.Equal(4, item.Sets);
        Assert.Equal(8, item.Reps);
        Assert.Equal(12, item.RepsMax);
        Assert.Equal(80.0, item.Weight);
        Assert.Equal(7.5, item.Rpe);
        Assert.Equal(9.0, item.RpeMax);
        Assert.Equal(120, item.RestSeconds);
        Assert.Equal("Пауза на груди", item.Notes);
    }

    [Theory]
    [InlineData(0, 8, null, null, null, null, null)]        // Sets <= 0
    [InlineData(3, 0, null, null, null, null, null)]        // Reps <= 0
    [InlineData(3, 8, 8, null, null, null, null)]           // RepsMax == Reps
    [InlineData(3, 8, 6, null, null, null, null)]           // RepsMax < Reps
    [InlineData(3, 8, null, -5.0, null, null, null)]        // Negative weight
    [InlineData(3, 8, null, null, 5.0, null, null)]         // Rpe < 5.5
    [InlineData(3, 8, null, null, 7.3, null, null)]         // Rpe not half step
    [InlineData(3, 8, null, null, 10.5, null, null)]        // Rpe > 10.0
    [InlineData(3, 8, null, null, null, 8.0, null)]         // RpeMax without Rpe
    [InlineData(3, 8, null, null, 8.0, 8.0, null)]          // RpeMax == Rpe
    [InlineData(3, 8, null, null, 8.5, 8.0, null)]          // RpeMax < Rpe
    [InlineData(3, 8, null, null, 7.0, 8.3, null)]          // RpeMax not half step
    [InlineData(3, 8, null, null, null, null, -1)]          // Negative rest
    public async Task Add_InvalidValues_Throw(
        int sets, int reps, int? repsMax, double? weight, double? rpe, double? rpeMax, int? rest)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _repo.AddAsync(new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _benchId,
            Sets = sets,
            Reps = reps,
            RepsMax = repsMax,
            Weight = weight,
            Rpe = rpe,
            RpeMax = rpeMax,
            RestSeconds = rest
        }));
    }

    [Theory]
    [InlineData(5.5, null)]
    [InlineData(6.0, 7.5)]
    [InlineData(8.5, 9.0)]
    [InlineData(9.5, 10.0)]
    public async Task Add_ValidRpe_Succeeds(double rpe, double? rpeMax)
    {
        var id = await _repo.AddAsync(new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _benchId,
            Sets = 3,
            Reps = 8,
            Rpe = rpe,
            RpeMax = rpeMax
        });

        var loaded = await _repo.GetAsync(id);
        Assert.NotNull(loaded);
        Assert.Equal(rpe, loaded.Rpe);
        Assert.Equal(rpeMax, loaded.RpeMax);
    }

    [Fact]
    public async Task Update_ChangesValues()
    {
        var id = await AddAsync(_benchId);
        var item = (await _repo.GetAsync(id))!;
        item.Sets = 5;
        item.Weight = 77.5;
        item.RepsMax = 10;
        item.Rpe = 7.5;
        item.RpeMax = 8.5;

        await _repo.UpdateAsync(item);

        var loaded = (await _repo.GetAsync(id))!;
        Assert.Equal(5, loaded.Sets);
        Assert.Equal(77.5, loaded.Weight);
        Assert.Equal(10, loaded.RepsMax);
        Assert.Equal(7.5, loaded.Rpe);
        Assert.Equal(8.5, loaded.RpeMax);
    }

    [Fact]
    public async Task Move_Up_SwapsWithPrevious_AndKeepsOrderWithoutGaps()
    {
        var a = await AddAsync(_benchId);
        var b = await AddAsync(_squatId);

        await _repo.MoveAsync(b, -1);

        var list = await _repo.GetByWorkoutAsync(_workoutId);
        Assert.Equal([b, a], list.Select(x => x.Id));
        Assert.Equal([1, 2], list.Select(x => x.Order));
    }

    [Fact]
    public async Task Move_Down_SwapsWithNext()
    {
        var a = await AddAsync(_benchId);
        var b = await AddAsync(_squatId);

        await _repo.MoveAsync(a, +1);

        var list = await _repo.GetByWorkoutAsync(_workoutId);
        Assert.Equal([b, a], list.Select(x => x.Id));
    }

    [Fact]
    public async Task Move_AtEdges_DoesNothing()
    {
        var a = await AddAsync(_benchId);
        var b = await AddAsync(_squatId);

        await _repo.MoveAsync(a, -1);
        await _repo.MoveAsync(b, +1);

        var list = await _repo.GetByWorkoutAsync(_workoutId);
        Assert.Equal([a, b], list.Select(x => x.Id));
    }

    [Fact]
    public async Task Delete_RemovesItem()
    {
        var id = await AddAsync(_benchId);

        await _repo.DeleteAsync(id);

        Assert.Null(await _repo.GetAsync(id));
    }

    [Fact]
    public async Task DeleteWorkout_CascadesToPlannedExercises()
    {
        var id = await AddAsync(_benchId);

        await _programs.DeleteWorkoutAsync(_workoutId);

        Assert.Null(await _repo.GetAsync(id));
    }
}
