namespace Myonic.Core.Models;

public class SetEntry
{
    public int Id { get; set; }
    public int WorkoutExerciseId { get; set; }
    public int SetNumber { get; set; }

    public double Weight { get; set; }
    public int Reps { get; set; }
    public double? Rpe { get; set; }
    public int? RestSeconds { get; set; }
    public string? Comment { get; set; }
    public DateTime CompletedAt { get; set; }
}