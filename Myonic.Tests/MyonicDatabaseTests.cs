using Myonic.Core.Models;
using Myonic.Data;
using SQLite;

namespace Myonic.Tests;

public class MyonicDatabaseTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private SQLiteAsyncConnection _conn = null!;

    public async Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _conn = await _db.GetConnectionAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    [Fact]
    public async Task Migration_SetsLatestSchemaVersion()
    {
        var version = await _conn.ExecuteScalarAsync<int>("PRAGMA user_version");
        Assert.Equal(Myonic.Data.Migrations.Migrations.LatestVersion, version);
    }

    [Fact]
    public async Task Insert_AssignsId_AndRoundTripsValues()
    {
        var exercise = new Exercise { Name = "Bench press", Type = ExerciseType.Compound, IsCustom = true };
        await _conn.InsertAsync(exercise);

        Assert.True(exercise.Id > 0);

        var loaded = await _conn.GetAsync<Exercise>(exercise.Id);
        Assert.Equal("Bench press", loaded.Name);
        Assert.Equal(ExerciseType.Compound, loaded.Type);
        Assert.True(loaded.IsCustom);
    }

    [Fact]
    public async Task DateTime_RoundTrips()
    {
        var started = new DateTime(2026, 10, 7, 18, 30, 15);
        var session = new WorkoutSession { Title = "Upper", StartedAt = started };
        await _conn.InsertAsync(session);

        var loaded = await _conn.GetAsync<WorkoutSession>(session.Id);
        Assert.Equal(started, loaded.StartedAt);
        Assert.Null(loaded.FinishedAt);
    }

    [Fact]
    public async Task DeleteExercise_UsedInHistory_IsRejected()
    {
        var (exercise, _, _, _) = await SeedWorkoutAsync();

        await Assert.ThrowsAsync<SQLiteException>(() => _conn.DeleteAsync(exercise));
    }

    [Fact]
    public async Task DeleteSession_CascadesToExercisesAndSets()
    {
        var (_, session, _, _) = await SeedWorkoutAsync();

        await _conn.DeleteAsync(session);

        Assert.Equal(0, await _conn.Table<WorkoutExercise>().CountAsync());
        Assert.Equal(0, await _conn.Table<SetEntry>().CountAsync());
    }

    private async Task<(Exercise, WorkoutSession, WorkoutExercise, SetEntry)> SeedWorkoutAsync()
    {
        var exercise = new Exercise { Name = "Bench press", Type = ExerciseType.Compound };
        await _conn.InsertAsync(exercise);

        var session = new WorkoutSession { Title = "Upper", StartedAt = DateTime.Now };
        await _conn.InsertAsync(session);

        var workoutExercise = new WorkoutExercise
        {
            WorkoutSessionId = session.Id,
            ExerciseId = exercise.Id,
            Order = 1
        };
        await _conn.InsertAsync(workoutExercise);

        var set = new SetEntry
        {
            WorkoutExerciseId = workoutExercise.Id,
            SetNumber = 1,
            Weight = 80,
            Reps = 8,
            CompletedAt = DateTime.Now
        };
        await _conn.InsertAsync(set);

        return (exercise, session, workoutExercise, set);
    }
}