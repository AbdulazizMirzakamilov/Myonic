using Myonic.Core.Models;

namespace Myonic.Core.Repositories;

public class ActiveSessionSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}

public class SessionExerciseDetail
{
    public int WorkoutExerciseId { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public ExerciseType ExerciseType { get; set; }
    public int Order { get; set; }

    public int? PlannedSets { get; set; }
    public int? PlannedReps { get; set; }
    public int? PlannedRepsMax { get; set; }
    public double? PlannedWeight { get; set; }
    public double? PlannedRpe { get; set; }
    public double? PlannedRpeMax { get; set; }
    public string PlanText { get; set; } = string.Empty;

    public List<SetEntry> Sets { get; set; } = [];
}

public class WorkoutSessionDetail
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Comment { get; set; }
    public List<SessionExerciseDetail> Exercises { get; set; } = [];
}

public class PastWorkoutSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public int CompletedSetsCount { get; set; }
    public int ExerciseCount { get; set; }
}

public interface IWorkoutSessionRepository
{
    Task<ActiveSessionSummary?> GetActiveSessionSummaryAsync();
    Task<int> StartSessionFromProgramWorkoutAsync(int programWorkoutId);
    Task<WorkoutSessionDetail?> GetSessionDetailAsync(int sessionId);
    Task<SetEntry> AddSetAsync(int workoutExerciseId, double weight, int reps, double? rpe, string? comment = null);
    Task DeleteSetAsync(int setId);
    Task FinishSessionAsync(int sessionId, string? comment = null);
    Task DiscardSessionAsync(int sessionId);
    Task<IReadOnlyList<PastWorkoutSummary>> GetPastSessionsAsync();
}
