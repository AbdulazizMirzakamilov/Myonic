using Myonic.Core.Models;

namespace Myonic.Core.Validation;

public static class ProgramExerciseValidator
{
    public const double MinRpe = 5.5;
    public const double MaxRpe = 10.0;

    public static bool IsValidRpeValue(double rpe)
    {
        if (rpe < MinRpe || rpe > MaxRpe) return false;
        // Half-step check (5.5, 6.0, 6.5, etc.)
        return Math.Abs((rpe * 2.0) - Math.Round(rpe * 2.0)) < 1e-6;
    }

    public static void Validate(ProgramExercise exercise)
    {
        ArgumentNullException.ThrowIfNull(exercise);

        if (exercise.Sets <= 0)
            throw new ArgumentException("Количество подходов должно быть больше 0.", nameof(exercise.Sets));

        if (exercise.Reps <= 0)
            throw new ArgumentException("Количество повторений должно быть больше 0.", nameof(exercise.Reps));

        if (exercise.RepsMax.HasValue && exercise.RepsMax.Value <= exercise.Reps)
            throw new ArgumentException("Максимальное количество повторений должно быть строго больше начального.", nameof(exercise.RepsMax));

        if (exercise.Weight.HasValue && exercise.Weight.Value < 0)
            throw new ArgumentException("Вес не может быть отрицательным.", nameof(exercise.Weight));

        if (exercise.Rpe.HasValue && !IsValidRpeValue(exercise.Rpe.Value))
            throw new ArgumentException($"RPE должен быть от {MinRpe} до {MaxRpe} с шагом 0.5.", nameof(exercise.Rpe));

        if (exercise.RpeMax.HasValue)
        {
            if (!exercise.Rpe.HasValue)
                throw new ArgumentException("Нельзя указать верхний RPE без нижнего.", nameof(exercise.RpeMax));

            if (!IsValidRpeValue(exercise.RpeMax.Value))
                throw new ArgumentException($"RPE должен быть от {MinRpe} до {MaxRpe} с шагом 0.5.", nameof(exercise.RpeMax));

            if (exercise.RpeMax.Value <= exercise.Rpe.Value)
                throw new ArgumentException("Верхняя граница RPE должна быть строго больше нижней.", nameof(exercise.RpeMax));
        }

        if (exercise.RestSeconds.HasValue && exercise.RestSeconds.Value < 0)
            throw new ArgumentException("Время отдыха не может быть отрицательным.", nameof(exercise.RestSeconds));
    }
}
