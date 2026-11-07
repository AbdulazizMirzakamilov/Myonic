namespace Myonic.Core.Calculators;

public enum OneRepMaxFormula
{
    Epley,
    Brzycki,
    Lander,
    Lombardi,
    Mayhew,
    OConner,
    Wathan
}

public sealed record OneRepMaxResult(
    IReadOnlyDictionary<OneRepMaxFormula, double> ByFormula,
    double Average,
    bool IsLowAccuracy);

/// <summary>
/// Оценочный 1ПМ (жим лёжа). Среднее значение - это оценка, а не измеренный 1ПМ.
/// </summary>
public static class OneRepMaxCalculator
{
    public const int MaxReps = 12;
    public const int LowAccuracyAboveReps = 10;

    public static OneRepMaxResult Calculate(double weight, int reps)
    {
        if (weight <= 0)
            throw new ArgumentOutOfRangeException(nameof(weight), "Вес должен быть больше нуля.");
        if (reps < 1 || reps > MaxReps)
            throw new ArgumentOutOfRangeException(nameof(reps), $"Повторения: от 1 до {MaxReps}.");

        var byFormula = new Dictionary<OneRepMaxFormula, double>();
        foreach (var formula in Enum.GetValues<OneRepMaxFormula>())
            byFormula[formula] = Calculate(formula, weight, reps);

        return new OneRepMaxResult(
            byFormula,
            byFormula.Values.Average(),
            reps > LowAccuracyAboveReps);
    }

    public static double Calculate(OneRepMaxFormula formula, double weight, int reps)
    {
        // При одном повторении вес и есть 1ПМ
        if (reps == 1) return weight;

        return formula switch
        {
            OneRepMaxFormula.Epley => weight * (1 + reps / 30.0),
            OneRepMaxFormula.Brzycki => weight * 36.0 / (37 - reps),
            OneRepMaxFormula.Lander => 100 * weight / (101.3 - 2.67123 * reps),
            OneRepMaxFormula.Lombardi => weight * Math.Pow(reps, 0.10),
            OneRepMaxFormula.Mayhew => 100 * weight / (52.2 + 41.9 * Math.Exp(-0.055 * reps)),
            OneRepMaxFormula.OConner => weight * (1 + 0.025 * reps),
            OneRepMaxFormula.Wathan => 100 * weight / (48.8 + 53.8 * Math.Exp(-0.075 * reps)),
            _ => throw new ArgumentOutOfRangeException(nameof(formula))
        };
    }

    /// <summary>
    /// Проверяет, относится ли упражнение к «Большой тройке» пауэрлифтинга (жим лёжа, приседания, становая тяга).
    /// </summary>
    public static bool IsBigThree(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var lower = name.Trim().ToLowerInvariant().Replace('ё', 'е');

        // Приседания: "приседания со штангой", "присед", "фронтальные приседания", "squat"
        bool isSquat = lower.Contains("присед") || lower.Contains("squat");

        // Жим лёжа: "жим лежа", "жим штанги лежа", "bench press", "жим", "жим штанги"
        // Исключает: "жим ногами", "французский жим", "жим стоя"
        bool isBench = (lower.Contains("жим") && lower.Contains("лежа"))
                    || lower.Contains("bench press")
                    || lower == "жим"
                    || lower == "жим штанги";

        // Становая тяга: "становая тяга", "становая", "deadlift"
        // Исключает: "тяга штанги в наклоне", "тяга блока", "румынская тяга"
        bool isDeadlift = lower.Contains("становая") || lower.Contains("deadlift");

        return isSquat || isBench || isDeadlift;
    }

    /// <summary>
    /// Рассчитывает средний 1ПМ по всем 7 формулам для упражнений «Большой тройки».
    /// Возвращает 0, если вес некорректен или повторений > MaxReps (12).
    /// </summary>
    public static double CalculateBigThreeAverage1Rm(double weight, int reps)
    {
        if (weight <= 0 || reps <= 0) return 0;
        if (reps == 1) return weight;
        if (reps > MaxReps) return 0;

        var result = Calculate(weight, reps);
        return Math.Round(result.Average, 1);
    }

    /// <summary>
    /// Округляет вес до ближайшего шага блинов (по умолчанию 2,5 кг - стандартный шаг 1,25 кг дисков с двух сторон).
    /// </summary>
    public static double RoundToPlateStep(double weight, double step = 2.5)
    {
        if (weight <= 0 || step <= 0) return weight;
        return Math.Round(weight / step, MidpointRounding.AwayFromZero) * step;
    }
}
