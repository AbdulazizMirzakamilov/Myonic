namespace Myonic.Core.Models;

public class ProgramWorkout
{
    public int Id { get; set; }
    public int ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public DayOfWeek? DefaultDayOfWeek { get; set; }   // подсказка для расписания
}