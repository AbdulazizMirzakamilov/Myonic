using System.Text.Json;
using Myonic.Core.Models;
using Myonic.Data;
using Myonic.Data.Services;
using Xunit;

namespace Myonic.Tests;

public class DataBackupServiceTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private readonly string _backupsDir;
    private MyonicDatabase _database = null!;
    private DataBackupService _service = null!;

    public DataBackupServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"myonic_test_backup_{Guid.NewGuid():N}.db3");
        _backupsDir = Path.Combine(Path.GetTempPath(), $"myonic_test_backups_{Guid.NewGuid():N}");
    }

    public async Task InitializeAsync()
    {
        _database = new MyonicDatabase(_dbPath);
        _service = new DataBackupService(_database);
        Directory.CreateDirectory(_backupsDir);

        // Ensure database initialized with migrations
        await _database.GetConnectionAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();

        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }

        if (Directory.Exists(_backupsDir))
        {
            try { Directory.Delete(_backupsDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task ExportToJsonAsync_ReturnsValidJsonWithAllData()
    {
        var db = await _database.GetConnectionAsync();

        // Get an existing exercise
        var exercise = await db.Table<Exercise>().FirstAsync();

        // Arrange: Insert some test data with valid foreign keys
        var session = new WorkoutSession
        {
            Title = "Грудь + Трицепс",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            FinishedAt = DateTime.UtcNow,
            Comment = "Отличная тренировка"
        };
        await db.InsertAsync(session);

        var workoutExercise = new WorkoutExercise
        {
            WorkoutSessionId = session.Id,
            ExerciseId = exercise.Id,
            Order = 1
        };
        await db.InsertAsync(workoutExercise);

        var set = new SetEntry
        {
            WorkoutExerciseId = workoutExercise.Id,
            SetNumber = 1,
            Weight = 100,
            Reps = 5,
            Rpe = 8.5
        };
        await db.InsertAsync(set);

        var measurement = new BodyMeasurement
        {
            Date = DateTime.Today,
            WeightKg = 82.5,
            BicepsCm = 41.0,
            ChestCm = 112.0
        };
        await db.InsertAsync(measurement);

        var goal = new Goal
        {
            Title = "Жим 120 кг",
            Type = GoalType.ExerciseWeight,
            StartValue = 100,
            TargetValue = 120,
            CurrentValue = 105,
            Status = GoalStatus.Active
        };
        await db.InsertAsync(goal);

        var exportPath = Path.Combine(_backupsDir, "test_export.json");

        // Act
        var resultPath = await _service.ExportToJsonAsync(exportPath);

        // Assert
        Assert.True(File.Exists(resultPath));
        var jsonContent = await File.ReadAllTextAsync(resultPath);
        Assert.False(string.IsNullOrWhiteSpace(jsonContent));

        var root = JsonSerializer.Deserialize<BackupDataRoot>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(root);
        Assert.NotNull(root.Metadata);
        Assert.Equal("Myonic", root.Metadata.AppName);
        Assert.True(root.Metadata.TotalWorkoutsCount >= 1);
        Assert.True(root.Metadata.TotalSetsCount >= 1);
        Assert.True(root.Metadata.TotalMeasurementsCount >= 1);
        Assert.True(root.Metadata.TotalGoalsCount >= 1);

        Assert.Contains(root.Sessions, s => s.Title == "Грудь + Трицепс");
        Assert.Contains(root.SetEntries, s => s.Weight == 100 && s.Reps == 5);
        Assert.Contains(root.BodyMeasurements, m => m.WeightKg == 82.5);
        Assert.Contains(root.Goals, g => g.Title == "Жим 120 кг");
    }

    [Fact]
    public async Task ImportFromJsonAsync_RoundTripRestoresData()
    {
        var db = await _database.GetConnectionAsync();

        // Arrange: Insert initial data
        var session = new WorkoutSession
        {
            Title = "Ноги + Плечи",
            StartedAt = DateTime.UtcNow.AddDays(-2),
            FinishedAt = DateTime.UtcNow.AddDays(-2).AddHours(1)
        };
        await db.InsertAsync(session);

        var goal = new Goal
        {
            Title = "Присед 160 кг",
            Type = GoalType.ExerciseWeight,
            StartValue = 140,
            TargetValue = 160,
            CurrentValue = 150
        };
        await db.InsertAsync(goal);

        var exportPath = Path.Combine(_backupsDir, "roundtrip_export.json");
        await _service.ExportToJsonAsync(exportPath);
        var json = await File.ReadAllTextAsync(exportPath);

        // Clear data
        await _service.ClearAllUserDataAsync(keepDefaultExercises: false);

        var sessionsAfterClear = await db.Table<WorkoutSession>().ToListAsync();
        var goalsAfterClear = await db.Table<Goal>().ToListAsync();
        Assert.Empty(sessionsAfterClear);
        Assert.Empty(goalsAfterClear);

        // Act: Import from JSON
        var summary = await _service.ImportFromJsonAsync(json);

        // Assert
        Assert.True(summary.Success);
        Assert.True(summary.SessionsCount >= 1);
        Assert.True(summary.GoalsCount >= 1);

        var restoredSessions = await db.Table<WorkoutSession>().ToListAsync();
        Assert.Contains(restoredSessions, s => s.Title == "Ноги + Плечи");

        var restoredGoals = await db.Table<Goal>().ToListAsync();
        Assert.Contains(restoredGoals, g => g.Title == "Присед 160 кг");
    }

    [Fact]
    public async Task ImportFromJsonAsync_CorruptedJson_ReturnsFailureSummary()
    {
        var result = await _service.ImportFromJsonAsync("This is totally not JSON!");
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task CreateSqliteBackupAsync_And_RestoreSqliteBackupAsync_RestoresState()
    {
        var db = await _database.GetConnectionAsync();

        var goal = new Goal
        {
            Title = "Становая 200 кг",
            Type = GoalType.ExerciseWeight,
            StartValue = 180,
            TargetValue = 200
        };
        await db.InsertAsync(goal);

        // Act: Create SQLite backup
        var backupInfo = await _service.CreateSqliteBackupAsync(_backupsDir);

        Assert.True(File.Exists(backupInfo.FilePath));
        Assert.True(backupInfo.SizeBytes > 0);
        Assert.EndsWith(".db3", backupInfo.FileName);

        // Wipe data in original database
        await _service.ClearAllUserDataAsync(keepDefaultExercises: true);
        var goalsAfterWipe = await (await _database.GetConnectionAsync()).Table<Goal>().ToListAsync();
        Assert.Empty(goalsAfterWipe);

        // Restore from backup
        var restored = await _service.RestoreSqliteBackupAsync(backupInfo.FilePath);
        Assert.True(restored);

        var conn = await _database.GetConnectionAsync();
        var restoredGoals = await conn.Table<Goal>().ToListAsync();
        Assert.Contains(restoredGoals, g => g.Title == "Становая 200 кг");
    }

    [Fact]
    public async Task ClearAllUserDataAsync_KeepsDefaultExercises()
    {
        var db = await _database.GetConnectionAsync();

        // Custom exercise
        var custom = new Exercise
        {
            Name = "Моё кастомное упражнение",
            Type = ExerciseType.Isolation,
            IsCustom = true
        };
        await db.InsertAsync(custom);

        var goal = new Goal { Title = "Цель для удаления", StartValue = 10, TargetValue = 20 };
        await db.InsertAsync(goal);

        // Act: Clear data keeping default exercises
        await _service.ClearAllUserDataAsync(keepDefaultExercises: true);

        var remainingExercises = await db.Table<Exercise>().ToListAsync();
        Assert.NotEmpty(remainingExercises);
        Assert.All(remainingExercises, e => Assert.False(e.IsCustom));
        Assert.DoesNotContain(remainingExercises, e => e.Name == "Моё кастомное упражнение");

        var remainingGoals = await db.Table<Goal>().ToListAsync();
        Assert.Empty(remainingGoals);
    }

    [Fact]
    public async Task GetAvailableBackupsAsync_ListsFilesAndAllowsDeletion()
    {
        // Create 2 backups with distinct millisecond timestamps
        var b1 = await _service.CreateSqliteBackupAsync(_backupsDir);
        await Task.Delay(20);
        var b2 = await _service.CreateSqliteBackupAsync(_backupsDir);

        var list = await _service.GetAvailableBackupsAsync(_backupsDir);
        Assert.True(list.Count >= 2);
        Assert.Contains(list, x => x.FileName == b1.FileName);
        Assert.Contains(list, x => x.FileName == b2.FileName);

        // Delete one
        var deleted = await _service.DeleteBackupFileAsync(b1.FilePath);
        Assert.True(deleted);

        var listAfterDelete = await _service.GetAvailableBackupsAsync(_backupsDir);
        Assert.DoesNotContain(listAfterDelete, x => x.FileName == b1.FileName);
    }
}
