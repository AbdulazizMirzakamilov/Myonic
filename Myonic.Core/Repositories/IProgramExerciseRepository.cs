using Myonic.Core.Models;

namespace Myonic.Core.Repositories;

/// <summary>Упражнение в шаблоне тренировки вместе с названием из библиотеки.</summary>
public class ProgramExerciseDetails
{
    public int Id { get; set; }
    public int ProgramWorkoutId { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
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
    Task<IReadOnlyList<ProgramExerciseDetails>> GetByWorkoutAsync(int programWorkoutId);
    Task<ProgramExercise?> GetAsync(int id);
    Task<int> AddAsync(ProgramExercise item);
    Task UpdateAsync(ProgramExercise item);
    Task DeleteAsync(int id);

    /// <summary>direction: -1 вверх, +1 вниз. На краю списка ничего не делает.</summary>
    Task MoveAsync(int id, int direction);
}
