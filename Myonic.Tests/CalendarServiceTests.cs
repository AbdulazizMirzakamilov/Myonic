using Myonic.Core.Calendar;
using Myonic.Core.Models;
using Myonic.Data;
using Myonic.Data.Repositories;
using Myonic.Data.Services;

namespace Myonic.Tests;

public class CalendarServiceTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-calendar-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private WorkoutSessionRepository _sessionRepo = null!;
    private WorkoutScheduleRepository _scheduleRepo = null!;
    private CalendarService _calendarService = null!;

    private int _programId;
    private int _programWorkoutId;
    private int _exerciseId;

    public async Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _sessionRepo = new WorkoutSessionRepository(_db);
        _scheduleRepo = new WorkoutScheduleRepository(_db);
        _calendarService = new CalendarService(_sessionRepo, _scheduleRepo);

        var conn = await _db.GetConnectionAsync();

        var program = new TrainingProgram { Name = "Powerbuilding", IsArchived = false };
        await conn.InsertAsync(program);
        _programId = program.Id;

        var workout = new ProgramWorkout
        {
            ProgramId = program.Id,
            Name = "Грудь и трицепс",
            DefaultDayOfWeek = DayOfWeek.Monday,
            Order = 1
        };
        await conn.InsertAsync(workout);
        _programWorkoutId = workout.Id;

        var ex = await conn.Table<Exercise>().FirstAsync();
        _exerciseId = ex.Id;

        var pe = new ProgramExercise
        {
            ProgramWorkoutId = workout.Id,
            ExerciseId = ex.Id,
            Order = 1,
            Sets = 4,
            Reps = 8,
            Weight = 90
        };
        await conn.InsertAsync(pe);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    [Fact]
    public async Task FuturePlannedWorkout_ReturnsPlannedStatus()
    {
        // Find next Monday in the future
        var date = DateTime.Today.AddDays(1);
        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(1);
        }

        var workouts = await _calendarService.GetWorkoutsForDateAsync(date);

        Assert.Single(workouts);
        Assert.Equal(CalendarWorkoutStatus.Planned, workouts[0].Status);
        Assert.Equal("Грудь и трицепс", workouts[0].Title);
        Assert.True(workouts[0].CanStart);
        Assert.False(workouts[0].CanViewDetails);
    }

    [Fact]
    public async Task PastUncompletedWorkout_ReturnsMissedStatus()
    {
        // Find past Monday (e.g. 2 weeks ago)
        var date = DateTime.Today.AddDays(-1);
        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(-1);
        }

        var workouts = await _calendarService.GetWorkoutsForDateAsync(date);

        Assert.Single(workouts);
        Assert.Equal(CalendarWorkoutStatus.Missed, workouts[0].Status);
        Assert.Equal("Грудь и трицепс", workouts[0].Title);
        Assert.True(workouts[0].CanStart);
        Assert.False(workouts[0].CanViewDetails);
    }

    [Fact]
    public async Task CompletedWorkout_ReturnsCompletedStatus_AndNoDuplicatePlannedCard()
    {
        // Completed today
        var sessionId = await _sessionRepo.StartEmptySessionAsync("Грудь и трицепс");
        await _sessionRepo.AddExerciseToSessionAsync(sessionId, _exerciseId);
        await _sessionRepo.FinishSessionAsync(sessionId);

        // Schedule explicit workout for today
        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(new ScheduledWorkout
        {
            ProgramWorkoutId = _programWorkoutId,
            Date = DateTime.Today,
            Status = ScheduledWorkoutStatus.Completed
        });

        var workouts = await _calendarService.GetWorkoutsForDateAsync(DateTime.Today);

        // Should return ONLY 1 workout item (Completed), no duplicate Planned/Missed card
        Assert.Single(workouts);
        Assert.Equal(CalendarWorkoutStatus.Completed, workouts[0].Status);
        Assert.Equal("Грудь и трицепс", workouts[0].Title);
        Assert.True(workouts[0].CanViewDetails);
        Assert.False(workouts[0].CanStart);
    }

    [Fact]
    public async Task RescheduledWorkout_DoesNotShowFalseMissedOnOriginalDate()
    {
        var pastMonday = DateTime.Today.AddDays(-7);
        while (pastMonday.DayOfWeek != DayOfWeek.Monday)
        {
            pastMonday = pastMonday.AddDays(-1);
        }

        // Reschedule Monday's workout to Wednesday
        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(new ScheduledWorkout
        {
            ProgramWorkoutId = _programWorkoutId,
            OriginalDate = pastMonday,
            Date = pastMonday.AddDays(2), // Wednesday
            Status = ScheduledWorkoutStatus.Planned
        });

        // Check original date (Monday): must NOT show a missed workout
        var mondayWorkouts = await _calendarService.GetWorkoutsForDateAsync(pastMonday);
        Assert.Empty(mondayWorkouts);

        // Check new date (Wednesday): must show the workout
        var wednesdayWorkouts = await _calendarService.GetWorkoutsForDateAsync(pastMonday.AddDays(2));
        Assert.Single(wednesdayWorkouts);
    }

    [Fact]
    public async Task CancelledWorkout_DoesNotShowOnDate()
    {
        var futureMonday = DateTime.Today.AddDays(1);
        while (futureMonday.DayOfWeek != DayOfWeek.Monday)
        {
            futureMonday = futureMonday.AddDays(1);
        }

        // Cancel workout on this date
        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(new ScheduledWorkout
        {
            ProgramWorkoutId = _programWorkoutId,
            Date = futureMonday,
            Status = ScheduledWorkoutStatus.Cancelled
        });

        var workouts = await _calendarService.GetWorkoutsForDateAsync(futureMonday);
        Assert.Empty(workouts);
    }

    [Fact]
    public async Task GetIndicatorsForRange_ReturnsCorrectIndicators()
    {
        var fromDate = DateTime.Today.AddDays(-7);
        var toDate = DateTime.Today.AddDays(7);

        // Create completed session today
        var sessionId = await _sessionRepo.StartEmptySessionAsync("Тест индикаторов");
        await _sessionRepo.FinishSessionAsync(sessionId);

        var indicators = await _calendarService.GetIndicatorsForRangeAsync(fromDate, toDate);

        Assert.NotNull(indicators);
        Assert.True(indicators.ContainsKey(DateTime.Today));
        Assert.True(indicators[DateTime.Today].HasCompleted);
    }

    [Fact]
    public async Task MultipleCompletedWorkoutsOnSameDay_ReturnsAllSessions()
    {
        var today = DateTime.Today;
        var s1 = await _sessionRepo.StartEmptySessionAsync("Утренняя тренировка");
        await _sessionRepo.FinishSessionAsync(s1);

        var s2 = await _sessionRepo.StartEmptySessionAsync("Вечерняя тренировка");
        await _sessionRepo.FinishSessionAsync(s2);

        var workouts = await _calendarService.GetWorkoutsForDateAsync(today);

        Assert.Equal(2, workouts.Count(w => w.Status == CalendarWorkoutStatus.Completed));
        Assert.Contains(workouts, w => w.Title == "Утренняя тренировка");
        Assert.Contains(workouts, w => w.Title == "Вечерняя тренировка");
    }

    [Fact]
    public async Task CompletedWorkout_LoadsExerciseComparisonDetails()
    {
        var today = DateTime.Today;
        var sessionId = await _sessionRepo.StartSessionFromProgramWorkoutAsync(_programWorkoutId);
        var session = await _sessionRepo.GetSessionDetailAsync(sessionId);
        Assert.NotNull(session);
        var we = session.Exercises.First();

        // Add 2 sets completed
        await _sessionRepo.AddSetAsync(we.WorkoutExerciseId, 90, 8, 8.0);
        await _sessionRepo.AddSetAsync(we.WorkoutExerciseId, 92.5, 7, 9.0);
        await _sessionRepo.FinishSessionAsync(sessionId);

        var workouts = await _calendarService.GetWorkoutsForDateAsync(today);
        var completed = workouts.FirstOrDefault(w => w.SessionId == sessionId);

        Assert.NotNull(completed);
        Assert.NotEmpty(completed.CompletedExercises);
        var exItem = completed.CompletedExercises.First();
        Assert.Equal(2, exItem.SetsSummary.Count);
        Assert.Contains("90", exItem.SetsSummary[0]);
    }
}
