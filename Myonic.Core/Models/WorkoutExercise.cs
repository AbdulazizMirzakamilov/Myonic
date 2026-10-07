namespace Myonic.Core.Models;

public class WorkoutExercise
{
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public int ExerciseId { get; set; }
    public int Order { get; set; }

    // Снимок плана; null, если упражнение добавлено на лету
    public int? PlannedSets { get; set; }
    public int? PlannedReps { get; set; }
    public double? PlannedWeight { get; set; }
    public double? PlannedRpe { get; set; }
    public int? PlannedRestSeconds { get; set; }

    public bool IsSkipped { get; set; }
    public string? Notes { get; set; }
}