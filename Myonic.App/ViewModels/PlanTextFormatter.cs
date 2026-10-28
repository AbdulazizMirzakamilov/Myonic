namespace Myonic.ViewModels;

internal static class PlanTextFormatter
{
    public static string Format(int sets, int reps, int? repsMax, double? weight, double? rpe, double? rpeMax, int? restSeconds)
    {
        var repsStr = repsMax is not null ? $"{reps}–{repsMax}" : reps.ToString();
        var parts = new List<string> { $"{sets} × {repsStr}" };
        if (weight is not null) parts.Add($"{weight.Value:0.##} кг");
        if (rpe is not null)
        {
            var rpeStr = rpeMax is not null ? $"{rpe.Value:0.#}–{rpeMax.Value:0.#}" : $"{rpe.Value:0.#}";
            parts.Add($"RPE {rpeStr}");
        }
        if (restSeconds is not null) parts.Add($"отдых {restSeconds} с");
        return string.Join(" · ", parts);
    }
}
