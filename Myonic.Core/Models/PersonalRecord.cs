namespace Myonic.Core.Models;

public class PersonalRecord
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public RecordType Type { get; set; }
    public double Weight { get; set; }
    public DateTime AchievedAt { get; set; }
    public int? SetEntryId { get; set; }    // null = добавлен вручную
}