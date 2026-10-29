using Myonic.Core.Models;
using Myonic.Data;
using Myonic.Data.Repositories;

namespace Myonic.Tests;

public class WorkoutSessionRepositoryTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-session-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private WorkoutSessionRepository _sessionRepo = null!;
    private int _programWorkoutId;
    private int _exerciseId;

    public async Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _sessionRepo = new WorkoutSessionRepository(_db);

        var conn = await _db.GetConnectionAsync();

        var program = new TrainingProgram { Name = "Push Pull Legs" };
        await conn.InsertAsync(program);

        var workout = new ProgramWorkout
        {
            ProgramId = program.Id,
            Name = "Push Day",
            DefaultDayOfWeek = DayOfWeek.Monday
        };
        await conn.InsertAsync(workout);
        _programWorkoutId = workout.Id;

        var ex = await conn.Table<Exercise>().FirstAsync();
        _exerciseId = ex.Id;

        var pe = new ProgramExercise
        {
            ProgramWorkoutId = _programWorkoutId,
            ExerciseId = _exerciseId,
            Order = 1,
            Sets = 3,
            Reps = 8,
            RepsMax = 10,
            Weight = 80,
            Rpe = 7.5,
            RpeMax = 8.5
        };
        await conn.InsertAsync(pe);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    [Fact]
    public async Task StartSession_CopiesProgramExercisesWithPlanSnapshots()
    {
        var sessionId = await _sessionRepo.StartSessionFromProgramWorkoutAsync(_programWorkoutId);

        var detail = await _sessionRepo.GetSessionDetailAsync(sessionId);
        Assert.NotNull(detail);
        Assert.Equal("Push Day", detail.Title);
        Assert.Null(detail.FinishedAt);
        Assert.Single(detail.Exercises);

        var ex = detail.Exercises[0];
        Assert.Equal(3, ex.PlannedSets);
        Assert.Equal(8, ex.PlannedReps);
        Assert.Equal(10, ex.PlannedRepsMax);
        Assert.Equal(80, ex.PlannedWeight);
        Assert.Equal(7.5, ex.PlannedRpe);
        Assert.Equal(8.5, ex.PlannedRpeMax);
        Assert.Equal("3 × 8–10, 80 кг @ 7.5–8.5", ex.PlanText);
    }

    [Fact]
    public async Task AddSet_AssignsSequentialNumbers_AndStoresValues()
    {
        var sessionId = await _sessionRepo.StartSessionFromProgramWorkoutAsync(_programWorkoutId);
        var detail = await _sessionRepo.GetSessionDetailAsync(sessionId);
        var weId = detail!.Exercises[0].WorkoutExerciseId;

        var s1 = await _sessionRepo.AddSetAsync(weId, 80, 8, 8.0);
        var s2 = await _sessionRepo.AddSetAsync(weId, 80, 9, 8.5);

        Assert.Equal(1, s1.SetNumber);
        Assert.Equal(2, s2.SetNumber);

        var updatedDetail = await _sessionRepo.GetSessionDetailAsync(sessionId);
        var sets = updatedDetail!.Exercises[0].Sets;
        Assert.Equal(2, sets.Count);
        Assert.Equal(80, sets[0].Weight);
        Assert.Equal(8, sets[0].Reps);
        Assert.Equal(8.0, sets[0].Rpe);
    }

    [Fact]
    public async Task DeleteSet_RecompactsNumbers()
    {
        var sessionId = await _sessionRepo.StartSessionFromProgramWorkoutAsync(_programWorkoutId);
        var detail = await _sessionRepo.GetSessionDetailAsync(sessionId);
        var weId = detail!.Exercises[0].WorkoutExerciseId;

        var s1 = await _sessionRepo.AddSetAsync(weId, 80, 8, null);
        var s2 = await _sessionRepo.AddSetAsync(weId, 80, 8, null);
        var s3 = await _sessionRepo.AddSetAsync(weId, 80, 8, null);

        await _sessionRepo.DeleteSetAsync(s2.Id);

        var updatedDetail = await _sessionRepo.GetSessionDetailAsync(sessionId);
        var sets = updatedDetail!.Exercises[0].Sets;
        Assert.Equal(2, sets.Count);
        Assert.Equal(1, sets[0].SetNumber);
        Assert.Equal(s1.Id, sets[0].Id);
        Assert.Equal(2, sets[1].SetNumber);
        Assert.Equal(s3.Id, sets[1].Id);
    }

    [Fact]
    public async Task FinishSession_SetsFinishedAt_AndAppearsInPastSessions()
    {
        var sessionId = await _sessionRepo.StartSessionFromProgramWorkoutAsync(_programWorkoutId);
        var detail = await _sessionRepo.GetSessionDetailAsync(sessionId);
        await _sessionRepo.AddSetAsync(detail!.Exercises[0].WorkoutExerciseId, 80, 8, 8.0);

        var active = await _sessionRepo.GetActiveSessionSummaryAsync();
        Assert.NotNull(active);
        Assert.Equal(sessionId, active.Id);

        await _sessionRepo.FinishSessionAsync(sessionId, "Отличная тренировка!");

        var activeAfterFinish = await _sessionRepo.GetActiveSessionSummaryAsync();
        Assert.Null(activeAfterFinish);

        var pastSessions = await _sessionRepo.GetPastSessionsAsync();
        Assert.Single(pastSessions);
        Assert.Equal(sessionId, pastSessions[0].Id);
        Assert.Equal("Push Day", pastSessions[0].Title);
        Assert.Equal(1, pastSessions[0].CompletedSetsCount);
        Assert.Equal(1, pastSessions[0].ExerciseCount);
    }

    [Fact]
    public async Task DiscardSession_DeletesEntireSession()
    {
        var sessionId = await _sessionRepo.StartSessionFromProgramWorkoutAsync(_programWorkoutId);
        var detail = await _sessionRepo.GetSessionDetailAsync(sessionId);
        await _sessionRepo.AddSetAsync(detail!.Exercises[0].WorkoutExerciseId, 80, 8, null);

        await _sessionRepo.DiscardSessionAsync(sessionId);

        var loaded = await _sessionRepo.GetSessionDetailAsync(sessionId);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task StartEmptySession_AllowsAddingExercisesAndCompleting()
    {
        var sessionId = await _sessionRepo.StartEmptySessionAsync("Фулбоди экспресс");
        Assert.True(sessionId > 0);

        var detail = await _sessionRepo.GetSessionDetailAsync(sessionId);
        Assert.NotNull(detail);
        Assert.Equal("Фулбоди экспресс", detail.Title);
        Assert.Empty(detail.Exercises);

        var addedExercise = await _sessionRepo.AddExerciseToSessionAsync(sessionId, _exerciseId);
        Assert.NotNull(addedExercise);
        Assert.Equal(1, addedExercise.Order);
        Assert.Equal(_exerciseId, addedExercise.ExerciseId);

        var updatedDetail = await _sessionRepo.GetSessionDetailAsync(sessionId);
        Assert.Single(updatedDetail!.Exercises);

        await _sessionRepo.AddSetAsync(addedExercise.WorkoutExerciseId, 100, 5, 8.5);
        await _sessionRepo.FinishSessionAsync(sessionId);

        var past = await _sessionRepo.GetPastSessionsAsync();
        Assert.Contains(past, p => p.Id == sessionId && p.CompletedSetsCount == 1);
    }

    [Fact]
    public async Task GetSessionsForDate_And_GetCompletedSessionDates_ReturnCorrectData()
    {
        var sessionId = await _sessionRepo.StartEmptySessionAsync("Утренняя тренировка");
        var added = await _sessionRepo.AddExerciseToSessionAsync(sessionId, _exerciseId);
        await _sessionRepo.AddSetAsync(added.WorkoutExerciseId, 70, 10, null);
        await _sessionRepo.FinishSessionAsync(sessionId);

        var today = DateTime.UtcNow.Date;
        var dates = await _sessionRepo.GetCompletedSessionDatesAsync(today.AddDays(-1), today.AddDays(1));
        Assert.Contains(today, dates);

        var daySessions = await _sessionRepo.GetSessionsForDateAsync(today);
        Assert.Contains(daySessions, s => s.Id == sessionId && s.Title == "Утренняя тренировка");
    }

    [Fact]
    public async Task GetExerciseHistory_ReturnsCorrectHistoryAndMaxStats()
    {
        // Session 1: 80kg x 8, 80kg x 7
        var session1Id = await _sessionRepo.StartEmptySessionAsync("Грудь 1");
        var we1 = await _sessionRepo.AddExerciseToSessionAsync(session1Id, _exerciseId);
        await _sessionRepo.AddSetAsync(we1.WorkoutExerciseId, 80, 8, 8.0);
        await _sessionRepo.AddSetAsync(we1.WorkoutExerciseId, 80, 7, 8.5);
        await _sessionRepo.FinishSessionAsync(session1Id);

        // Session 2: 90kg x 5, 100kg x 1
        var session2Id = await _sessionRepo.StartEmptySessionAsync("Грудь 2");
        var we2 = await _sessionRepo.AddExerciseToSessionAsync(session2Id, _exerciseId);
        await _sessionRepo.AddSetAsync(we2.WorkoutExerciseId, 90, 5, 8.0);
        await _sessionRepo.AddSetAsync(we2.WorkoutExerciseId, 100, 1, 9.5);
        await _sessionRepo.FinishSessionAsync(session2Id);

        var history = await _sessionRepo.GetExerciseHistoryAsync(_exerciseId);

        Assert.Equal(_exerciseId, history.ExerciseId);
        Assert.Equal(2, history.TotalSessionsCount);
        Assert.Equal(4, history.TotalSetsCount);
        Assert.Equal(100.0, history.AllTimeMaxWeight);
        // Epley for 80x8: 80 * (1 + 8/30.0) = 101.3. For 90x5: 90 * (1 + 5/30.0) = 105.0. For 100x1 = 100. Best is 105.0
        Assert.Equal(105.0, history.AllTimeBest1Rm);
        Assert.Equal(2, history.Sessions.Count);
        Assert.Equal("Грудь 2", history.Sessions[0].WorkoutTitle);
        Assert.Equal(2, history.Sessions[0].Sets.Count);
    }
}
