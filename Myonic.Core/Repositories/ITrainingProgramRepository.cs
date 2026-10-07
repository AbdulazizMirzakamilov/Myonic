using Myonic.Core.Models;

namespace Myonic.Core.Repositories;

public class ProgramSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int WorkoutCount { get; set; }
}

public interface ITrainingProgramRepository
{
    // Программы
    Task<IReadOnlyList<ProgramSummary>> GetSummariesAsync();
    Task<TrainingProgram?> GetProgramAsync(int id);
    Task<int> AddProgramAsync(TrainingProgram program);
    Task UpdateProgramAsync(TrainingProgram program);
    Task DeleteProgramAsync(int id);

    // Тренировки внутри программы
    Task<IReadOnlyList<ProgramWorkout>> GetWorkoutsAsync(int programId);
    Task<ProgramWorkout?> GetWorkoutAsync(int id);
    Task<int> AddWorkoutAsync(ProgramWorkout workout);
    Task UpdateWorkoutAsync(ProgramWorkout workout);
    Task DeleteWorkoutAsync(int id);
}