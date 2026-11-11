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

    [Fact]
    public async Task ArchivedProgram_WorkoutsAreExcludedFromPlannedSchedule()
    {
        var conn = await _db.GetConnectionAsync();
        var archivedProgram = new TrainingProgram { Name = "Old Archived Program", IsArchived = true };
        await conn.InsertAsync(archivedProgram);

        var workout = new ProgramWorkout
        {
            ProgramId = archivedProgram.Id,
            Name = "Archived Workout",
            DefaultDayOfWeek = DayOfWeek.Friday,
            Order = 1
        };
        await conn.InsertAsync(workout);

        // Find next Friday
        var friday = DateTime.Today;
        while (friday.DayOfWeek != DayOfWeek.Friday)
        {
            friday = friday.AddDays(1);
        }

        var planned = await _scheduleRepo.GetPlannedWorkoutsForDateAsync(friday);
        Assert.DoesNotContain(planned, p => p.ProgramWorkoutId == workout.Id);

        var (defaultDays, _) = await _scheduleRepo.GetPlannedDayIndicatorsForWeekAsync(friday.AddDays(-6), friday);
        Assert.DoesNotContain(DayOfWeek.Friday, defaultDays);
    }

    [Fact]
    public async Task RescheduleWorkoutAsync_MovesWorkoutToNewDate_AndExcludesFromOriginalDate()
    {
        var wednesday = DateTime.Today;
        while (wednesday.DayOfWeek != DayOfWeek.Wednesday)
        {
            wednesday = wednesday.AddDays(1);
        }
        var thursday = wednesday.AddDays(1);

        // Before reschedule: Wednesday has workout
        var beforeWed = await _scheduleRepo.GetPlannedWorkoutsForDateAsync(wednesday);
        Assert.Single(beforeWed);

        // Reschedule Wednesday -> Thursday
        await _scheduleRepo.RescheduleWorkoutAsync(_programWorkoutId, wednesday, thursday);

        // After reschedule: Wednesday has 0, Thursday has 1
        var afterWed = await _scheduleRepo.GetPlannedWorkoutsForDateAsync(wednesday);
        Assert.Empty(afterWed);

        var afterThu = await _scheduleRepo.GetPlannedWorkoutsForDateAsync(thursday);
        Assert.Single(afterThu);
        Assert.Equal("Upper Body A", afterThu[0].WorkoutName);
        Assert.Equal(thursday.Date, afterThu[0].ScheduledDate);
        Assert.Equal(wednesday.Date, afterThu[0].OriginalDate);
    }

    [Fact]
    public async Task CancelScheduledWorkout_ExcludesFromSchedule()
    {
        var wednesday = DateTime.Today;
        while (wednesday.DayOfWeek != DayOfWeek.Wednesday)
        {
            wednesday = wednesday.AddDays(1);
        }

        await _scheduleRepo.CancelScheduledWorkoutAsync(_programWorkoutId, wednesday);

        var planned = await _scheduleRepo.GetPlannedWorkoutsForDateAsync(wednesday);
        Assert.Empty(planned);
    }

    [Fact]
    public async Task EnsureScheduledWorkoutsForRange_InsertsRowsInSqliteTable()
    {
        var conn = await _db.GetConnectionAsync();
        var fromDate = DateTime.Today;
        var toDate = DateTime.Today.AddDays(21);

        await _scheduleRepo.EnsureScheduledWorkoutsForRangeAsync(fromDate, toDate);

        var count = await conn.Table<ScheduledWorkout>().CountAsync();
        Assert.True(count > 0);
    }
}
