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
        Assert.False(workouts[0].CanStart);
        Assert.True(workouts[0].IsFuture);
        Assert.False(workouts[0].CanViewDetails);
    }

    [Fact]
    public async Task TodayPlannedWorkout_CanStartIsTrue()
    {
        var conn = await _db.GetConnectionAsync();
        var workoutToday = new ProgramWorkout
        {
            ProgramId = _programId,
            Name = "Тренировка на сегодня",
            DefaultDayOfWeek = DateTime.Today.DayOfWeek,
            Order = 2
        };
        await conn.InsertAsync(workoutToday);

        var workouts = await _calendarService.GetWorkoutsForDateAsync(DateTime.Today);

        var todayItem = workouts.FirstOrDefault(w => w.ProgramWorkoutId == workoutToday.Id);
        Assert.NotNull(todayItem);
        Assert.True(todayItem.CanStart);
        Assert.False(todayItem.IsFuture);
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

        // Past uncompleted workout is returned as Missed
        Assert.Single(workouts);
        Assert.Equal(CalendarWorkoutStatus.Missed, workouts[0].Status);
        Assert.True(workouts[0].IsMissed);
        Assert.False(workouts[0].IsCompleted);
        Assert.False(workouts[0].IsPlanned);
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

        // Should return ONLY 1 workout item (Completed), no duplicate Planned card
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

        // Reschedule Monday's workout to Wednesday (future)
        var futureWednesday = DateTime.Today.AddDays(1);
        while (futureWednesday.DayOfWeek != DayOfWeek.Wednesday)
        {
            futureWednesday = futureWednesday.AddDays(1);
        }

        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(new ScheduledWorkout
        {
            ProgramWorkoutId = _programWorkoutId,
            OriginalDate = pastMonday,
            Date = futureWednesday,
            Status = ScheduledWorkoutStatus.Planned
        });

        // Check original date (past Monday): must NOT show a workout
        var mondayWorkouts = await _calendarService.GetWorkoutsForDateAsync(pastMonday);
        Assert.Empty(mondayWorkouts);

        // Check new date (future Wednesday): must show the workout
        var wednesdayWorkouts = await _calendarService.GetWorkoutsForDateAsync(futureWednesday);
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
        Assert.False(indicators[DateTime.Today].HasMissed);
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

    [Fact]
    public async Task RescheduleWorkout_PersistsToSqlite_AndSurvivesDbReconnection()
    {
        var nextMonday = DateTime.Today.AddDays(1);
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }

        var targetThursday = nextMonday.AddDays(3); // Thursday of same week

        // Reschedule Monday to Thursday
        await _scheduleRepo.RescheduleWorkoutAsync(_programWorkoutId, nextMonday, targetThursday);

        // Disconnect and reopen new context from the same file
        var reopenedDb = new MyonicDatabase(_path);
        var newScheduleRepo = new WorkoutScheduleRepository(reopenedDb);
        var newCalendar = new CalendarService(new WorkoutSessionRepository(reopenedDb), newScheduleRepo);

        // Verify Monday has 0 workouts (rescheduled away)
        var mondayWorkouts = await newCalendar.GetWorkoutsForDateAsync(nextMonday);
        Assert.Empty(mondayWorkouts);

        // Verify Thursday has the rescheduled workout
        var thursdayWorkouts = await newCalendar.GetWorkoutsForDateAsync(targetThursday);
        Assert.Single(thursdayWorkouts);
        Assert.Equal("Грудь и трицепс", thursdayWorkouts[0].Title);
        Assert.Equal(CalendarWorkoutStatus.Planned, thursdayWorkouts[0].Status);

        await reopenedDb.DisposeAsync();
    }

    [Fact]
    public async Task RescheduledWorkout_DoesNotAffectOtherProgramDays()
    {
        // Add a second workout in the program for Friday
        var conn = await _db.GetConnectionAsync();
        var workoutFriday = new ProgramWorkout
        {
            ProgramId = _programId,
            Name = "Спина и бицепс",
            DefaultDayOfWeek = DayOfWeek.Friday,
            Order = 2
        };
        await conn.InsertAsync(workoutFriday);

        var nextMonday = DateTime.Today.AddDays(1);
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }
        var nextFriday = nextMonday.AddDays(4);

        // Reschedule Monday to Tuesday
        await _scheduleRepo.RescheduleWorkoutAsync(_programWorkoutId, nextMonday, nextMonday.AddDays(1));

        // Friday workout should remain completely untouched on Friday
        var fridayWorkouts = await _calendarService.GetWorkoutsForDateAsync(nextFriday);
        Assert.Single(fridayWorkouts);
        Assert.Equal("Спина и бицепс", fridayWorkouts[0].Title);
    }

    [Fact]
    public async Task CompletedWorkout_OnPastDate_NeverDisplaysAsMissed()
    {
        var pastMonday = DateTime.Today.AddDays(-7);
        while (pastMonday.DayOfWeek != DayOfWeek.Monday)
        {
            pastMonday = pastMonday.AddDays(-1);
        }

        // Complete a session on past Monday
        var conn = await _db.GetConnectionAsync();
        var session = new WorkoutSession
        {
            Title = "Грудь и трицепс",
            StartedAt = pastMonday.AddHours(10).ToUniversalTime(),
            FinishedAt = pastMonday.AddHours(11).ToUniversalTime()
        };
        await conn.InsertAsync(session);

        var workouts = await _calendarService.GetWorkoutsForDateAsync(pastMonday);

        Assert.Single(workouts);
        Assert.Equal(CalendarWorkoutStatus.Completed, workouts[0].Status);
        Assert.False(workouts[0].IsMissed);
        Assert.True(workouts[0].IsCompleted);
    }

    [Fact]
    public async Task GetIndicatorsForRange_PastUncompletedDay_HasMissedIsTrue()
    {
        var pastMonday = DateTime.Today.AddDays(-7);
        while (pastMonday.DayOfWeek != DayOfWeek.Monday)
        {
            pastMonday = pastMonday.AddDays(-1);
        }

        var indicators = await _calendarService.GetIndicatorsForRangeAsync(pastMonday.AddDays(-1), pastMonday.AddDays(1));

        Assert.True(indicators.ContainsKey(pastMonday));
        Assert.True(indicators[pastMonday].HasMissed);
        Assert.False(indicators[pastMonday].HasCompleted);
    }

    [Fact]
    public async Task EdgeCases_YearTransition_MonthEnd_LeapYear()
    {
        // 1. Month end transition (e.g. October 31 -> November 1)
        var oct31 = new DateTime(2025, 10, 31);
        var nov1 = new DateTime(2025, 11, 1);
        var res1 = await _scheduleRepo.RescheduleWorkoutAsync(_programWorkoutId, oct31, nov1);
        Assert.Equal(nov1.Date, res1.Date);

        // 2. Year boundary transition (December 31 -> January 1)
        var dec31 = new DateTime(2025, 12, 31);
        var jan1 = new DateTime(2026, 1, 1);
        var res2 = await _scheduleRepo.RescheduleWorkoutAsync(_programWorkoutId, dec31, jan1);
        Assert.Equal(jan1.Date, res2.Date);

        // 3. Leap year (February 28, 2028 -> February 29, 2028)
        var feb28 = new DateTime(2028, 2, 28);
        var feb29 = new DateTime(2028, 2, 29);
        var res3 = await _scheduleRepo.RescheduleWorkoutAsync(_programWorkoutId, feb28, feb29);
        Assert.Equal(feb29.Date, res3.Date);
    }

    [Fact]
    public async Task ScheduledWorkouts_ArePersistedToSqliteTable_CountIsGreaterThanZero()
    {
        var conn = await _db.GetConnectionAsync();

        // Ensure range materialization
        await _scheduleRepo.EnsureScheduledWorkoutsForRangeAsync(DateTime.Today, DateTime.Today.AddDays(14));

        var count = await conn.Table<ScheduledWorkout>().CountAsync();
        Assert.True(count > 0);
    }
}
