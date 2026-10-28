namespace Myonic.ViewModels;

internal static class PlanTextFormatter
{
    public static string Format(int sets, int reps, double? weight, double? rpe, int? restSeconds)
    {
        var parts = new List<string> { $"{sets} × {reps}" };
        if (weight is not null) parts.Add($"{weight.Value:0.##} кг");
        if (rpe is not null) parts.Add($"RPE {rpe.Value:0.#}");
        if (restSeconds is not null) parts.Add($"отдых {restSeconds} с");
        return string.Join(" · ", parts);
    }
}