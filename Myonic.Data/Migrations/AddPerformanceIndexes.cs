namespace Myonic.Data.Migrations;

public static class AddPerformanceIndexes
{
    public static readonly IReadOnlyList<string> Statements =
    [
        "CREATE INDEX IF NOT EXISTS IX_WorkoutSession_FinishedAt ON WorkoutSession(FinishedAt)",
        "CREATE INDEX IF NOT EXISTS IX_ScheduledWorkout_OriginalDate ON ScheduledWorkout(OriginalDate)",
        "CREATE INDEX IF NOT EXISTS IX_PersonalRecord_AchievedAt ON PersonalRecord(AchievedAt)",
        "CREATE INDEX IF NOT EXISTS IX_Goal_TargetDate ON Goal(TargetDate)"
    ];
}
