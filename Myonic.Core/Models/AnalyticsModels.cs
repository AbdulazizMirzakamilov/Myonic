namespace Myonic.Core.Models;

public enum AnalyticsPeriod
{
    Days30 = 30,
    Days90 = 90,
    Days180 = 180,
    All = 0
}

public class WeeklyVolumePoint
{
    public DateTime WeekStartDate { get; set; }
    public string Label { get; set; } = string.Empty;
    public double VolumeKg { get; set; }
    public int WorkoutsCount { get; set; }

    public WeeklyVolumePoint() { }

    public WeeklyVolumePoint(DateTime weekStartDate, string label, double volumeKg, int workoutsCount)
    {
        WeekStartDate = weekStartDate;
        Label = label;
        VolumeKg = volumeKg;
        WorkoutsCount = workoutsCount;
    }
}

public class MuscleGroupDistributionItem
{
    public string Name { get; set; } = string.Empty;
    public int SetsCount { get; set; }
    public double VolumeKg { get; set; }
    public double Percentage { get; set; }

    public MuscleGroupDistributionItem() { }

    public MuscleGroupDistributionItem(string name, int setsCount, double volumeKg, double percentage)
    {
        Name = name;
        SetsCount = setsCount;
        VolumeKg = volumeKg;
        Percentage = percentage;
    }

    public string FormattedVolume => VolumeKg >= 1000 ? $"{VolumeKg / 1000.0:F1} т" : $"{VolumeKg:N0} кг";

    public string SubtitleText => $"{SetsCount} подх • {FormattedVolume} ({Percentage:F1}%)";

    public double ProgressFraction => Math.Clamp(Percentage / 100.0, 0.02, 1.0);
}

public class AnalyticsSummary
{
    public AnalyticsPeriod Period { get; set; } = AnalyticsPeriod.Days30;
    public int TotalWorkouts { get; set; }
    public double TotalVolumeKg { get; set; }
    public int TotalSets { get; set; }
    public int TotalReps { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public double WorkoutsPerWeek { get; set; }
    public int PersonalRecordsCount { get; set; }
    public int CompoundSetsCount { get; set; }
    public int IsolationSetsCount { get; set; }

    public List<WeeklyVolumePoint> WeeklyPoints { get; set; } = [];
    public List<MuscleGroupDistributionItem> MuscleDistribution { get; set; } = [];

    public string FormattedTotalVolume
    {
        get
        {
            if (TotalVolumeKg >= 1000)
            {
                return $"{(TotalVolumeKg / 1000.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} т";
            }
            return $"{TotalVolumeKg:N0} кг";
        }
    }

    public string FormattedAverageDuration
    {
        get
        {
            if (AverageDuration.TotalMinutes < 1) return "0 мин";
            if (AverageDuration.TotalHours >= 1)
            {
                return $"{(int)AverageDuration.TotalHours} ч {AverageDuration.Minutes} мин";
            }
            return $"{(int)AverageDuration.TotalMinutes} мин";
        }
    }
}
