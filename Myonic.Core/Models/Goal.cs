namespace Myonic.Core.Models;

public class Goal
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public GoalType Type { get; set; }
    public int? ExerciseId { get; set; }
    public string? ExerciseName { get; set; }
    public string? BodyPart { get; set; }
    public double StartValue { get; set; }
    public double TargetValue { get; set; }
    public double CurrentValue { get; set; }
    public DateTime? TargetDate { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AchievedAt { get; set; }

    public double ProgressFraction
    {
        get
        {
            if (Status == GoalStatus.Achieved) return 1.0;
            if (Math.Abs(TargetValue - StartValue) < 0.0001)
                return CurrentValue >= TargetValue ? 1.0 : 0.0;

            if (TargetValue > StartValue)
            {
                return Math.Clamp((CurrentValue - StartValue) / (TargetValue - StartValue), 0.0, 1.0);
            }
            else
            {
                return Math.Clamp((StartValue - CurrentValue) / (StartValue - TargetValue), 0.0, 1.0);
            }
        }
    }

    public int ProgressPercent => (int)Math.Round(ProgressFraction * 100);

    public double RemainingValue
    {
        get
        {
            if (TargetValue >= StartValue)
                return Math.Max(0, Math.Round(TargetValue - CurrentValue, 1));
            else
                return Math.Max(0, Math.Round(CurrentValue - TargetValue, 1));
        }
    }

    public bool IsTargetMet
    {
        get
        {
            if (TargetValue >= StartValue)
                return CurrentValue >= TargetValue;
            else
                return CurrentValue <= TargetValue;
        }
    }
}
