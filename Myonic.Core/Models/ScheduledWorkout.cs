namespace Myonic.Core.Models;

public class ScheduledWorkout
{
    public int Id { get; set; }
    public int ProgramWorkoutId { get; set; }

    /// <summary>Только дата (время 00:00).</summary>
    public DateTime Date { get; set; }

    /// <summary>Заполнено, если тренировку переносили.</summary>
    public DateTime? OriginalDate { get; set; }

    public ScheduledWorkoutStatus Status { get; set; }
}