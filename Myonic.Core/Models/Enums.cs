namespace Myonic.Core.Models;

public enum ExerciseType
{
    Compound,   // базовое
    Isolation   // изолирующее
}

public enum MuscleGroup
{
    None = 0,               // Не указана / Все
    Chest = 1,              // Грудь
    Back = 2,               // Спина
    Shoulders = 3,          // Плечи
    Biceps = 4,             // Бицепс
    Triceps = 5,            // Трицепс
    Forearms = 6,           // Предплечья
    Quadriceps = 7,         // Квадрицепсы
    Hamstrings = 8,         // Задняя поверхность бедра
    Glutes = 9,             // Ягодицы
    Calves = 10,            // Икры
    Abs = 11                // Пресс
}

public static class MuscleGroupExtensions
{
    public static string ToDisplayText(this MuscleGroup group) => group switch
    {
        MuscleGroup.Chest => "Грудь",
        MuscleGroup.Back => "Спина",
        MuscleGroup.Shoulders => "Плечи",
        MuscleGroup.Biceps => "Бицепс",
        MuscleGroup.Triceps => "Трицепс",
        MuscleGroup.Forearms => "Предплечья",
        MuscleGroup.Quadriceps => "Квадрицепсы",
        MuscleGroup.Hamstrings => "Задняя поверхность бедра",
        MuscleGroup.Glutes => "Ягодицы",
        MuscleGroup.Calves => "Икры",
        MuscleGroup.Abs => "Пресс",
        _ => "Не указана"
    };

    public static string ToShortDisplayText(this MuscleGroup group) => group switch
    {
        MuscleGroup.Hamstrings => "Бёдра (задняя)",
        _ => group.ToDisplayText()
    };
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
