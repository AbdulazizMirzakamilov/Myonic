namespace Myonic.Core.Models;

public class ProgramExercise
{
    public int Id { get; set; }
    public int ProgramWorkoutId { get; set; }
    public int ExerciseId { get; set; }
    public int Order { get; set; }

    public int Sets { get; set; }
    public int Reps { get; set; }
    public double? Weight { get; set; }
    public double? Rpe { get; set; }
    public int? RestSeconds { get; set; }
    public string? Notes { get; set; }
}