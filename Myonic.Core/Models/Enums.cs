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

public enum GoalType
{
    ExerciseWeight = 0,      // Силовая цель (максимальный рабочий вес в упражнении)
    BodyWeight = 1,          // Вес тела (набор или сброс веса)
    BodyMeasurement = 2,     // Обхват части тела (бицепс, талия, грудь и др.)
    WorkoutsFrequency = 3    // Количество тренировок в месяц
}

public enum GoalStatus
{
    Active = 0,              // В процессе
    Achieved = 1,            // Достигнута
    Archived = 2             // В архиве
}
