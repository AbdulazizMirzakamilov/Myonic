using Myonic.Core.Models;

namespace Myonic.Core.Repositories;

public class ProgramExerciseDetail
{
    public int Id { get; set; }
    public int ProgramWorkoutId { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public ExerciseType ExerciseType { get; set; }
    public int Order { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int? RepsMax { get; set; }
    public double? Weight { get; set; }
    public double? Rpe { get; set; }
    public double? RpeMax { get; set; }
    public int? RestSeconds { get; set; }
    public string? Notes { get; set; }
}

public interface IProgramExerciseRepository
{
    Task<IReadOnlyList<ProgramExerciseDetail>> GetByWorkoutIdAsync(int workoutId);
    Task<ProgramExercise?> GetByIdAsync(int id);
    Task<int> AddAsync(ProgramExercise exercise);
    Task UpdateAsync(ProgramExercise exercise);
    Task DeleteAsync(int id);
    Task MoveUpAsync(int id);
    Task MoveDownAsync(int id);
}
