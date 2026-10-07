using Myonic.Core.Models;
using Myonic.Data;
using Myonic.Data.Repositories;

namespace Myonic.Tests;

public class TrainingProgramRepositoryTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private TrainingProgramRepository _repo = null!;

    public Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _repo = new TrainingProgramRepository(_db);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    [Fact]
    public async Task AddProgram_TrimsName_AndAssignsId()
    {
        var id = await _repo.AddProgramAsync(new TrainingProgram { Name = "  Моя программа  " });

        var loaded = await _repo.GetProgramAsync(id);
        Assert.NotNull(loaded);
        Assert.Equal("Моя программа", loaded.Name);
    }

    [Fact]
    public async Task AddProgram_EmptyName_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repo.AddProgramAsync(new TrainingProgram { Name = " " }));
    }

    [Fact]
    public async Task AddProgram_DuplicateName_IgnoringCase_Throws()
    {
        await _repo.AddProgramAsync(new TrainingProgram { Name = "Upper Lower" });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repo.AddProgramAsync(new TrainingProgram { Name = "upper lower" }));
    }

    [Fact]
    public async Task UpdateProgram_KeepingOwnName_IsAllowed()
    {
        var id = await _repo.AddProgramAsync(new TrainingProgram { Name = "Моя программа" });
        var program = (await _repo.GetProgramAsync(id))!;
        program.Description = "Новое описание";

        await _repo.UpdateProgramAsync(program);

        Assert.Equal("Новое описание", (await _repo.GetProgramAsync(id))!.Description);
    }

    [Fact]
    public async Task Summaries_ContainWorkoutCount_AndAreSortedByName()
    {
        var b = await _repo.AddProgramAsync(new TrainingProgram { Name = "бета" });
        var a = await _repo.AddProgramAsync(new TrainingProgram { Name = "Альфа" });
        await _repo.AddWorkoutAsync(new ProgramWorkout { ProgramId = a, Name = "Upper" });
        await _repo.AddWorkoutAsync(new ProgramWorkout { ProgramId = a, Name = "Lower" });

        var summaries = await _repo.GetSummariesAsync();

        Assert.Equal(["Альфа", "бета"], summaries.Select(s => s.Name));
        Assert.Equal(2, summaries[0].WorkoutCount);
        Assert.Equal(0, summaries[1].WorkoutCount);
    }

    [Fact]
    public async Task AddWorkout_AssignsIncreasingOrder()
    {
        var programId = await _repo.AddProgramAsync(new TrainingProgram { Name = "P" });

        var first = await _repo.AddWorkoutAsync(new ProgramWorkout { ProgramId = programId, Name = "A" });
        var second = await _repo.AddWorkoutAsync(new ProgramWorkout { ProgramId = programId, Name = "B" });

        var workouts = await _repo.GetWorkoutsAsync(programId);
        Assert.Equal([first, second], workouts.Select(w => w.Id));
        Assert.Equal([1, 2], workouts.Select(w => w.Order));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Sunday)]
    public async Task Workout_DefaultDayOfWeek_RoundTrips(DayOfWeek? day)
    {
        var programId = await _repo.AddProgramAsync(new TrainingProgram { Name = "P" });
        var id = await _repo.AddWorkoutAsync(
            new ProgramWorkout { ProgramId = programId, Name = "A", DefaultDayOfWeek = day });

        var loaded = await _repo.GetWorkoutAsync(id);

        Assert.Equal(day, loaded!.DefaultDayOfWeek);
    }

    [Fact]
    public async Task DeleteProgram_CascadesToWorkouts_ButKeepsHistory()
    {
        var programId = await _repo.AddProgramAsync(new TrainingProgram { Name = "P" });
        var workoutId = await _repo.AddWorkoutAsync(new ProgramWorkout { ProgramId = programId, Name = "Upper" });

        var conn = await _db.GetConnectionAsync();
        var scheduled = new ScheduledWorkout { ProgramWorkoutId = workoutId, Date = DateTime.Today };
        await conn.InsertAsync(scheduled);
        var session = new WorkoutSession
        {
            ScheduledWorkoutId = scheduled.Id,
            Title = "Upper",
            StartedAt = DateTime.Now
        };
        await conn.InsertAsync(session);

        await _repo.DeleteProgramAsync(programId);

        Assert.Null(await _repo.GetWorkoutAsync(workoutId));
        Assert.Equal(0, await conn.Table<ScheduledWorkout>().CountAsync());

        var kept = await conn.GetAsync<WorkoutSession>(session.Id);
        Assert.Null(kept.ScheduledWorkoutId);
        Assert.Equal("Upper", kept.Title);
    }
}