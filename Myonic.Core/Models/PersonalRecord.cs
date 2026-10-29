namespace Myonic.Core.Models;

public class PersonalRecord
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public RecordType Type { get; set; }
    public double Weight { get; set; }
    public int Reps { get; set; } = 1;
    public DateTime AchievedAt { get; set; }
    public int? SetEntryId { get; set; }    // null = добавлен вручную
}
