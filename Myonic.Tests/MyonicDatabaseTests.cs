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

    [Fact]
    public async Task BackupAsync_CreatesValidDatabaseCopy()
    {
        var backupPath = Path.Combine(Path.GetTempPath(), $"myonic-backup-{Guid.NewGuid():N}.db3");
        try
        {
            await _db.BackupAsync(backupPath);
            Assert.True(File.Exists(backupPath));
            var fileInfo = new FileInfo(backupPath);
            Assert.True(fileInfo.Length > 0);

            var backupConn = new SQLiteAsyncConnection(backupPath);
            var version = await backupConn.ExecuteScalarAsync<int>("PRAGMA user_version");
            Assert.Equal(Myonic.Data.Migrations.Migrations.LatestVersion, version);
            await backupConn.CloseAsync();
        }
        finally
        {
            if (File.Exists(backupPath))
                File.Delete(backupPath);
        }
    }

    [Fact]
    public async Task Pragma_IntegrityCheck_ReturnsOk()
    {
        var result = await _conn.ExecuteScalarAsync<string>("PRAGMA integrity_check");
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Pragma_ForeignKeyCheck_ReturnsNoViolations()
    {
        var rows = await _conn.QueryScalarsAsync<string>("PRAGMA foreign_key_check");
        Assert.Empty(rows);
    }

    [Fact]
    public async Task Migration6_CreatesBodyMeasurementAndGoalTables_WithIndexes()
    {
        // Check tables exist and can accept records
        var measurement = new BodyMeasurement
        {
            Date = DateTime.Today,
            WeightKg = 75.5,
            NeckCm = 38.0,
            WaistCm = 80.0,
            CreatedAt = DateTime.UtcNow
        };
        await _conn.InsertAsync(measurement);
        Assert.True(measurement.Id > 0);

        var loadedMeasurement = await _conn.GetAsync<BodyMeasurement>(measurement.Id);
        Assert.Equal(75.5, loadedMeasurement.WeightKg);

        var goal = new Goal
        {
            Title = "Жим 100 кг",
            Type = GoalType.ExerciseWeight,
            StartValue = 80,
            TargetValue = 100,
            CurrentValue = 85,
            CreatedAt = DateTime.UtcNow
        };
        await _conn.InsertAsync(goal);
        Assert.True(goal.Id > 0);

        var loadedGoal = await _conn.GetAsync<Goal>(goal.Id);
        Assert.Equal("Жим 100 кг", loadedGoal.Title);
        Assert.Equal(85, loadedGoal.CurrentValue);

        // Verify indexes exist
        var indexes = await _conn.QueryScalarsAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='index'");
        Assert.Contains("IX_BodyMeasurement_Date", indexes);
        Assert.Contains("IX_Goal_Status", indexes);
        Assert.Contains("IX_Goal_ExerciseId", indexes);
    }

    [Fact]
    public async Task Migration6_OnExistingDatabaseWithData_PreservesExistingRecords()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"myonic-mig-test-{Guid.NewGuid():N}.db3");
        MyonicDatabase? upgradedDb = null;
        try
        {
            // 1. Создаем базу версии 5 вручную
            var rawConn = new SQLiteAsyncConnection(tempDbPath, storeDateTimeAsTicks: false);
            await rawConn.ExecuteAsync("PRAGMA foreign_keys = ON");
            await rawConn.ExecuteScalarAsync<string>("PRAGMA journal_mode = WAL");

            // Применяем миграции с 1 по 5
            foreach (var mig in Myonic.Data.Migrations.Migrations.All.Where(m => m.Version <= 5).OrderBy(m => m.Version))
            {
                await rawConn.RunInTransactionAsync(conn =>
                {
                    foreach (var sql in mig.Statements)
                        conn.Execute(sql);
                    conn.Execute($"PRAGMA user_version = {mig.Version}");
                });
            }

            // Допустим, таблица BodyMeasurement уже существовала (как было в v5 через CreateTableAsync)
            await rawConn.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS BodyMeasurement (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Date TEXT NOT NULL,
                    WeightKg REAL NOT NULL,
                    CreatedAt TEXT NOT NULL
                )
            """);

            await rawConn.ExecuteAsync(
                "INSERT INTO BodyMeasurement (Date, WeightKg, CreatedAt) VALUES (?, ?, ?)",
                "2026-11-01", 82.5, "2026-11-01T10:00:00");

            var v5 = await rawConn.ExecuteScalarAsync<int>("PRAGMA user_version");
            Assert.Equal(5, v5);

            await rawConn.CloseAsync();

            // 2. Открываем через MyonicDatabase - это должно накатить миграции до актуальной версии
            upgradedDb = new MyonicDatabase(tempDbPath);
            var upgradedConn = await upgradedDb.GetConnectionAsync();

            var currentVer = await upgradedConn.ExecuteScalarAsync<int>("PRAGMA user_version");
            Assert.Equal(Myonic.Data.Migrations.Migrations.LatestVersion, currentVer);

            // Проверяем, что существующие данные не потерялись
            var count = await upgradedConn.Table<BodyMeasurement>().CountAsync();
            Assert.Equal(1, count);

            var item = await upgradedConn.Table<BodyMeasurement>().FirstAsync();
            Assert.Equal(82.5, item.WeightKg);

            // Проверяем целостность и внешние ключи
            var integrity = await upgradedConn.ExecuteScalarAsync<string>("PRAGMA integrity_check");
            Assert.Equal("ok", integrity);

            var fkErrors = await upgradedConn.QueryScalarsAsync<string>("PRAGMA foreign_key_check");
            Assert.Empty(fkErrors);
        }
        finally
        {
            if (upgradedDb != null)
                await upgradedDb.DisposeAsync();

            if (File.Exists(tempDbPath))
                File.Delete(tempDbPath);
        }
    }

    [Fact]
    public async Task Migration7_CreatesPerformanceIndexes()
    {
        var indexes = await _conn.QueryScalarsAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='index'");

        Assert.Contains("IX_WorkoutSession_FinishedAt", indexes);
        Assert.Contains("IX_ScheduledWorkout_OriginalDate", indexes);
        Assert.Contains("IX_PersonalRecord_AchievedAt", indexes);
        Assert.Contains("IX_Goal_TargetDate", indexes);
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