namespace Myonic.Core.Models;

public class BackupMetadata
{
    public int SchemaVersion { get; set; } = 1;
    public string AppVersion { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public string AppName { get; set; } = "Myonic";
    public int TotalWorkoutsCount { get; set; }
    public int TotalSetsCount { get; set; }
    public int TotalProgramsCount { get; set; }
    public int TotalExercisesCount { get; set; }
    public int TotalMeasurementsCount { get; set; }
    public int TotalGoalsCount { get; set; }
}

public class BackupDataRoot
{
    public BackupMetadata Metadata { get; set; } = new();
    public List<Exercise> Exercises { get; set; } = [];
    public List<TrainingProgram> Programs { get; set; } = [];
    public List<ProgramWorkout> Workouts { get; set; } = [];
    public List<ProgramExercise> ProgramExercises { get; set; } = [];
    public List<ScheduledWorkout> Schedules { get; set; } = [];
    public List<WorkoutSession> Sessions { get; set; } = [];
    public List<WorkoutExercise> WorkoutExercises { get; set; } = [];
    public List<SetEntry> SetEntries { get; set; } = [];
    public List<PersonalRecord> PersonalRecords { get; set; } = [];
    public List<BodyMeasurement> BodyMeasurements { get; set; } = [];
    public List<Goal> Goals { get; set; } = [];
}

public class BackupSummary
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ExercisesCount { get; set; }
    public int ProgramsCount { get; set; }
    public int SessionsCount { get; set; }
    public int SetsCount { get; set; }
    public int RecordsCount { get; set; }
    public int MeasurementsCount { get; set; }
    public int GoalsCount { get; set; }

    public string ToDisplayString() =>
        $"Импортировано:\n" +
        $"• Тренировок: {SessionsCount}\n" +
        $"• Подходов: {SetsCount}\n" +
        $"• Программ: {ProgramsCount}\n" +
        $"• Упражнений: {ExercisesCount}\n" +
        $"• Рекордов (PR): {RecordsCount}\n" +
        $"• Замеров тела: {MeasurementsCount}\n" +
        $"• Целей: {GoalsCount}";
}

public class BackupFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public string DateFormatted { get; set; } = string.Empty;
}
