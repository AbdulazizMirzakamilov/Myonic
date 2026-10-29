using System.Globalization;
using System.Text;

namespace Myonic.Core.Formatting;

public static class PlanTextFormatter
{
    public static string Format(int sets, int reps, int? repsMax, double? weight, double? rpe, double? rpeMax)
    {
        var sb = new StringBuilder();
        sb.Append(sets);
        sb.Append(" × ");
        sb.Append(reps);
        if (repsMax.HasValue && repsMax.Value > reps)
        {
            sb.Append('–');
            sb.Append(repsMax.Value);
        }

        if (weight.HasValue && weight.Value > 0)
        {
            sb.Append(", ");
            sb.Append(weight.Value.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append(" кг");
        }

        if (rpe.HasValue)
        {
            sb.Append(" @ ");
            sb.Append(rpe.Value.ToString("0.#", CultureInfo.InvariantCulture));
            if (rpeMax.HasValue && rpeMax.Value > rpe.Value)
            {
                sb.Append('–');
                sb.Append(rpeMax.Value.ToString("0.#", CultureInfo.InvariantCulture));
            }
        }

        return sb.ToString();
    }
}
