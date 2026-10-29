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
    OneRepMax = 0,   // Расчётный или 1-повторный максимум
    FiveRepMax = 1,  // Лучший вес на 5 повторений
    MaxWeight = 2    // Абсолютный максимальный рабочий вес
}
