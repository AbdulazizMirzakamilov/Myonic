using Myonic.Core.Models;
using Myonic.Data;
using Myonic.Data.Repositories;

namespace Myonic.Tests;

public class WorkoutScheduleRepositoryTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-schedule-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private WorkoutScheduleRepository _scheduleRepo = null!;
    private int _programWorkoutId;

    public async Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _scheduleRepo = new WorkoutScheduleRepository(_db);

        var conn = await _db.GetConnectionAsync();

        var program = new TrainingProgram { Name = "Powerbuilding 4D" };
        await conn.InsertAsync(program);

        var workout = new ProgramWorkout
        {
            ProgramId = program.Id,
            Name = "Upper Body A",
            DefaultDayOfWeek = DayOfWeek.Wednesday,
            Order = 1
        };
        await conn.InsertAsync(workout);
        _programWorkoutId = workout.Id;

        var ex = await conn.Table<Exercise>().FirstAsync();
        var pe = new ProgramExercise
        {
            ProgramWorkoutId = workout.Id,
            ExerciseId = ex.Id,
            Order = 1,
            Sets = 4,
            Reps = 6,
            Weight = 100,
            Rpe = 8
        };
        await conn.InsertAsync(pe);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    [Fact]
    public async Task GetPlannedWorkoutsForDate_ReturnsWorkoutMatchingDefaultDayOfWeek()
    {
        // Find next Wednesday
        var date = DateTime.Today;
        while (date.DayOfWeek != DayOfWeek.Wednesday)
        {
            date = date.AddDays(1);
        }

        var planned = await _scheduleRepo.GetPlannedWorkoutsForDateAsync(date);

        Assert.Single(planned);
        Assert.Equal("Upper Body A", planned[0].WorkoutName);
        Assert.Equal("Powerbuilding 4D", planned[0].ProgramName);
        Assert.Single(planned[0].Exercises);
        Assert.Equal("4 × 6, 100 кг @ 8", planned[0].Exercises[0].PlanText);
    }

    [Fact]
    public async Task GetAllAvailableProgramWorkouts_ReturnsAllWorkoutsAcrossPrograms()
    {
        var all = await _scheduleRepo.GetAllAvailableProgramWorkoutsAsync();
        Assert.NotEmpty(all);
        Assert.Contains(all, w => w.ProgramWorkoutId == _programWorkoutId);
    }
}
