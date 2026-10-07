namespace Myonic.Core.Models;

public class WorkoutSession
{
    public int Id { get; set; }
    public int? ScheduledWorkoutId { get; set; }   // null = тренировка вне расписания
    public string Title { get; set; } = string.Empty;   // снимок названия
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }      // null = ещё идёт, можно возобновить
    public string? Comment { get; set; }
}