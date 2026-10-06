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
}
