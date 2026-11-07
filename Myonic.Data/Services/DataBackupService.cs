using System.Text;
using System.Text.Json;
using Myonic.Core.Models;
using Myonic.Core.Services;

namespace Myonic.Data.Services;

public sealed class DataBackupService : IDataBackupService
{
    private static readonly byte[] SqliteHeaderMagic = Encoding.ASCII.GetBytes("SQLite format 3\0");

    private readonly MyonicDatabase _database;

    public DataBackupService(MyonicDatabase database)
    {
        _database = database;
    }

    public async Task<string> ExportToJsonAsync(string? targetFilePath = null)
    {
        var db = await _database.GetConnectionAsync();

        var exercises = await db.Table<Exercise>().ToListAsync();
        var programs = await db.Table<TrainingProgram>().ToListAsync();
        var workouts = await db.Table<ProgramWorkout>().ToListAsync();
        var programExercises = await db.Table<ProgramExercise>().ToListAsync();
        var schedules = await db.Table<ScheduledWorkout>().ToListAsync();
        var sessions = await db.Table<WorkoutSession>().ToListAsync();
        var workoutExercises = await db.Table<WorkoutExercise>().ToListAsync();
        var setEntries = await db.Table<SetEntry>().ToListAsync();
        var prs = await db.Table<PersonalRecord>().ToListAsync();
        var measurements = await db.Table<BodyMeasurement>().ToListAsync();
        var goals = await db.Table<Goal>().ToListAsync();

        var root = new BackupDataRoot
        {
            Metadata = new BackupMetadata
            {
                SchemaVersion = 1,
                AppVersion = "1.0",
                ExportedAt = DateTime.UtcNow,
                AppName = "Myonic",
                TotalWorkoutsCount = sessions.Count,
                TotalSetsCount = setEntries.Count,
                TotalProgramsCount = programs.Count,
                TotalExercisesCount = exercises.Count,
                TotalMeasurementsCount = measurements.Count,
                TotalGoalsCount = goals.Count
            },
            Exercises = exercises,
            Programs = programs,
            Workouts = workouts,
            ProgramExercises = programExercises,
            Schedules = schedules,
            Sessions = sessions,
            WorkoutExercises = workoutExercises,
            SetEntries = setEntries,
            PersonalRecords = prs,
            BodyMeasurements = measurements,
            Goals = goals
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(root, options);

        if (string.IsNullOrWhiteSpace(targetFilePath))
        {
            var baseDir = Path.GetDirectoryName(_database.DatabasePath) ?? "";
            var backupDir = Path.Combine(baseDir, "Backups");
            Directory.CreateDirectory(backupDir);
            targetFilePath = Path.Combine(backupDir, $"myonic_export_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json");
        }
        else
        {
            var parent = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }
        }

        await File.WriteAllTextAsync(targetFilePath, json, Encoding.UTF8);
        return targetFilePath;
    }

    public async Task<BackupSummary> ImportFromJsonAsync(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return new BackupSummary { Success = false, ErrorMessage = "Файл пуст или содержит некорректные данные." };
        }

        BackupDataRoot? root;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            root = JsonSerializer.Deserialize<BackupDataRoot>(jsonContent, options);
        }
        catch (Exception ex)
        {
            return new BackupSummary
            {
                Success = false,
                ErrorMessage = $"Ошибка разбора JSON: {ex.Message}"
            };
        }

        if (root == null || root.Metadata == null)
        {
            return new BackupSummary
            {
                Success = false,
                ErrorMessage = "Неверная структура резервной копии Myonic."
            };
        }

        var db = await _database.GetConnectionAsync();

        await db.ExecuteAsync("PRAGMA foreign_keys = OFF");
        try
        {
            // Clear current tables
            await db.ExecuteAsync("DELETE FROM SetEntry");
            await db.ExecuteAsync("DELETE FROM WorkoutExercise");
            await db.ExecuteAsync("DELETE FROM WorkoutSession");
            await db.ExecuteAsync("DELETE FROM ProgramExercise");
            await db.ExecuteAsync("DELETE FROM ProgramWorkout");
            await db.ExecuteAsync("DELETE FROM TrainingProgram");
            await db.ExecuteAsync("DELETE FROM ScheduledWorkout");
            await db.ExecuteAsync("DELETE FROM PersonalRecord");
            await db.ExecuteAsync("DELETE FROM BodyMeasurement");
            await db.ExecuteAsync("DELETE FROM Goal");
            await db.ExecuteAsync("DELETE FROM Exercise");

            // Bulk insert in topological order
            if (root.Exercises?.Count > 0)
                await db.InsertAllAsync(root.Exercises);

            if (root.Programs?.Count > 0)
                await db.InsertAllAsync(root.Programs);

            if (root.Workouts?.Count > 0)
                await db.InsertAllAsync(root.Workouts);

            if (root.ProgramExercises?.Count > 0)
                await db.InsertAllAsync(root.ProgramExercises);

            if (root.Schedules?.Count > 0)
                await db.InsertAllAsync(root.Schedules);

            if (root.Sessions?.Count > 0)
                await db.InsertAllAsync(root.Sessions);

            if (root.WorkoutExercises?.Count > 0)
                await db.InsertAllAsync(root.WorkoutExercises);

            if (root.SetEntries?.Count > 0)
                await db.InsertAllAsync(root.SetEntries);

            if (root.PersonalRecords?.Count > 0)
                await db.InsertAllAsync(root.PersonalRecords);

            if (root.BodyMeasurements?.Count > 0)
                await db.InsertAllAsync(root.BodyMeasurements);

            if (root.Goals?.Count > 0)
                await db.InsertAllAsync(root.Goals);

            return new BackupSummary
            {
                Success = true,
                ExercisesCount = root.Exercises?.Count ?? 0,
                ProgramsCount = root.Programs?.Count ?? 0,
                SessionsCount = root.Sessions?.Count ?? 0,
                SetsCount = root.SetEntries?.Count ?? 0,
                RecordsCount = root.PersonalRecords?.Count ?? 0,
                MeasurementsCount = root.BodyMeasurements?.Count ?? 0,
                GoalsCount = root.Goals?.Count ?? 0
            };
        }
        finally
        {
            await db.ExecuteAsync("PRAGMA foreign_keys = ON");
        }
    }

    public async Task<BackupFileInfo> CreateSqliteBackupAsync(string? targetDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            var baseDir = Path.GetDirectoryName(_database.DatabasePath) ?? "";
            targetDirectory = Path.Combine(baseDir, "Backups");
        }

        Directory.CreateDirectory(targetDirectory);
        var fileName = $"myonic_backup_{DateTime.Now:yyyyMMdd_HHmmss_fff}.db3";
        var destPath = Path.Combine(targetDirectory, fileName);

        await _database.BackupAsync(destPath);

        var fi = new FileInfo(destPath);
        return MapToFileInfo(fi);
    }

    public async Task<bool> RestoreSqliteBackupAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("Файл резервной копии не найден.", backupFilePath);
        }

        // Validate SQLite header
        using (var stream = File.OpenRead(backupFilePath))
        {
            if (stream.Length < 16)
            {
                throw new InvalidDataException("Файл повреждён или не является базой данных SQLite.");
            }

            var buffer = new byte[16];
            await stream.ReadExactlyAsync(buffer, 0, 16);
            if (!buffer.SequenceEqual(SqliteHeaderMagic))
            {
                throw new InvalidDataException("Файл не является корректной базой данных SQLite (неверная сигнатура заголовка).");
            }
        }

        // Close active database connection
        await _database.CloseConnectionAsync();

        // Delete temporary WAL files if present
        var dbPath = _database.DatabasePath;
        var walPath = dbPath + "-wal";
        var shmPath = dbPath + "-shm";

        if (File.Exists(walPath))
        {
            try { File.Delete(walPath); } catch { }
        }
        if (File.Exists(shmPath))
        {
            try { File.Delete(shmPath); } catch { }
        }

        // Copy backup to database path
        File.Copy(backupFilePath, dbPath, overwrite: true);

        // Reconnect and run migrations/checks
        await _database.GetConnectionAsync();

        return true;
    }

    public Task<IReadOnlyList<BackupFileInfo>> GetAvailableBackupsAsync(string? directory = null)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            var baseDir = Path.GetDirectoryName(_database.DatabasePath) ?? "";
            directory = Path.Combine(baseDir, "Backups");
        }

        if (!Directory.Exists(directory))
        {
            return Task.FromResult<IReadOnlyList<BackupFileInfo>>([]);
        }

        var dirInfo = new DirectoryInfo(directory);
        var files = dirInfo.GetFiles("*.*")
            .Where(f => f.Extension.Equals(".db3", StringComparison.OrdinalIgnoreCase)
                     || f.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(MapToFileInfo)
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupFileInfo>>(files);
    }

    public Task<bool> DeleteBackupFileAsync(string backupFilePath)
    {
        if (File.Exists(backupFilePath))
        {
            File.Delete(backupFilePath);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public async Task ClearAllUserDataAsync(bool keepDefaultExercises = true)
    {
        var db = await _database.GetConnectionAsync();

        await db.ExecuteAsync("PRAGMA foreign_keys = OFF");
        try
        {
            await db.ExecuteAsync("DELETE FROM SetEntry");
            await db.ExecuteAsync("DELETE FROM WorkoutExercise");
            await db.ExecuteAsync("DELETE FROM WorkoutSession");
            await db.ExecuteAsync("DELETE FROM ProgramExercise");
            await db.ExecuteAsync("DELETE FROM ProgramWorkout");
            await db.ExecuteAsync("DELETE FROM TrainingProgram");
            await db.ExecuteAsync("DELETE FROM ScheduledWorkout");
            await db.ExecuteAsync("DELETE FROM PersonalRecord");
            await db.ExecuteAsync("DELETE FROM BodyMeasurement");
            await db.ExecuteAsync("DELETE FROM Goal");

            if (keepDefaultExercises)
            {
                await db.ExecuteAsync("DELETE FROM Exercise WHERE IsCustom = 1");
            }
            else
            {
                await db.ExecuteAsync("DELETE FROM Exercise");
            }

            await db.ExecuteAsync("VACUUM");
        }
        finally
        {
            await db.ExecuteAsync("PRAGMA foreign_keys = ON");
        }
    }

    private static BackupFileInfo MapToFileInfo(FileInfo fi)
    {
        var sizeKb = fi.Length / 1024.0;
        var sizeText = sizeKb >= 1024
            ? $"{sizeKb / 1024.0:F2} МБ"
            : $"{Math.Max(1, Math.Round(sizeKb)):F0} КБ";

        return new BackupFileInfo
        {
            FilePath = fi.FullName,
            FileName = fi.Name,
            SizeBytes = fi.Length,
            CreatedAt = fi.CreationTime,
            SizeFormatted = sizeText,
            DateFormatted = fi.CreationTime.ToString("d MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("ru-RU"))
        };
    }
}
