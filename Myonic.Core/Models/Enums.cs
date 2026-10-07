namespace Myonic.Core.Models;

public enum ExerciseType
{
    Compound,   // базовое
    Isolation   // изолирующее
}

public enum ScheduledWorkoutStatus
{
    Planned,
    Completed,
    Cancelled
}

public enum RecordType
{
    OneRepMax,
    FiveRepMax
}