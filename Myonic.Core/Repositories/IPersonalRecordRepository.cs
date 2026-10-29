using Myonic.Core.Models;

namespace Myonic.Core.Repositories;

public class PersonalRecordItem
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public RecordType Type { get; set; }
    public double Weight { get; set; }
    public int Reps { get; set; }
    public DateTime AchievedAt { get; set; }
    public int? SetEntryId { get; set; }
    public bool IsManual => SetEntryId == null;
    public string SourceText => IsManual ? "Вручную" : "Тренировка";

    public string TypeDisplayName => Type switch
    {
        RecordType.MaxWeight => "Макс. вес",
        RecordType.OneRepMax => "1ПМ",
        RecordType.FiveRepMax => "5ПМ",
        _ => Type.ToString()
    };
}

public interface IPersonalRecordRepository
{
    Task<IReadOnlyList<PersonalRecordItem>> GetAllAsync();
    Task<IReadOnlyList<PersonalRecordItem>> GetByExerciseAsync(int exerciseId);
    Task<PersonalRecordItem?> GetBestRecordAsync(int exerciseId, RecordType type);
    Task<int> AddManualRecordAsync(int exerciseId, RecordType type, double weight, int reps, DateTime achievedAt);
    Task DeleteRecordAsync(int id);
    Task<IReadOnlyList<PersonalRecordItem>> CheckAndRecordNewPrsAsync(int workoutExerciseId, int setEntryId, double weight, int reps);
}
