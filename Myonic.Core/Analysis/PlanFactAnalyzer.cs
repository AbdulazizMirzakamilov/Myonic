using Myonic.Core.Formatting;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Core.Analysis;

public sealed record ExercisePlanFactAnalysis(
    int ExerciseId,
    string ExerciseName,
    bool HasPlan,
    string PlanText,
    int PlannedSets,
    int ActualSets,
    int PlannedRepsTotal,
    int ActualRepsTotal,
    double? PlannedWeight,
    double ActualMaxWeight,
    double ActualAvgWeight,
    double? PlannedVolume,
    double ActualVolume,
    double? PlannedRpe,
    double? ActualAvgRpe,
    int SetsCompliancePercent,
    int RepsCompliancePercent,
    int OverallCompliancePercent,
    string StatusBadgeText,
    string ComplianceColorHex);

public sealed record WorkoutPlanFactSummary(
    int TotalExercises,
    int PlannedExercises,
    bool HasPlan,
    int TotalPlannedSets,
    int TotalActualSets,
    int TotalPlannedReps,
    int TotalActualReps,
    double TotalPlannedVolume,
    double TotalActualVolume,
    int OverallCompliancePercent,
    string OverallStatusText,
    string OverallColorHex,
    IReadOnlyList<ExercisePlanFactAnalysis> Exercises);

public static class PlanFactAnalyzer
{
    public static WorkoutPlanFactSummary Analyze(WorkoutSessionDetail session)
    {
        var list = new List<ExercisePlanFactAnalysis>();
        int totalPlannedSets = 0;
        int totalActualSets = 0;
        int totalPlannedReps = 0;
        int totalActualReps = 0;
        double totalPlannedVolume = 0;
        double totalActualVolume = 0;
        int plannedExercisesCount = 0;

        foreach (var ex in session.Exercises)
        {
            var analysis = AnalyzeExercise(ex);
            list.Add(analysis);

            if (analysis.HasPlan)
            {
                plannedExercisesCount++;
                totalPlannedSets += analysis.PlannedSets;
                totalPlannedReps += analysis.PlannedRepsTotal;
                if (analysis.PlannedVolume.HasValue)
                    totalPlannedVolume += analysis.PlannedVolume.Value;
            }

            totalActualSets += analysis.ActualSets;
            totalActualReps += analysis.ActualRepsTotal;
            totalActualVolume += analysis.ActualVolume;
        }

        bool hasPlan = plannedExercisesCount > 0 && totalPlannedSets > 0;
        int overallPercent = 100;
        string statusText;
        string colorHex;

        if (hasPlan)
        {
            overallPercent = (int)Math.Round(list.Where(e => e.HasPlan).Average(e => e.OverallCompliancePercent));
            if (overallPercent >= 100)
            {
                statusText = "План выполнен полностью (100%)";
                colorHex = "#10B981"; // Emerald
            }
            else if (overallPercent >= 85)
            {
                statusText = $"План выполнен на {overallPercent}%";
                colorHex = "#CCFF00"; // Volt
            }
            else if (overallPercent >= 60)
            {
                statusText = $"План выполнен частично ({overallPercent}%)";
                colorHex = "#EAB308"; // Amber
            }
            else
            {
                statusText = $"План недовыполнен ({overallPercent}%)";
                colorHex = "#EF4444"; // Crimson
            }
        }
        else
        {
            statusText = "Свободная тренировка (без плана)";
            colorHex = "#38BDF8"; // Ice Blue
        }

        return new WorkoutPlanFactSummary(
            TotalExercises: session.Exercises.Count,
            PlannedExercises: plannedExercisesCount,
            HasPlan: hasPlan,
            TotalPlannedSets: totalPlannedSets,
            TotalActualSets: totalActualSets,
            TotalPlannedReps: totalPlannedReps,
            TotalActualReps: totalActualReps,
            TotalPlannedVolume: totalPlannedVolume,
            TotalActualVolume: totalActualVolume,
            OverallCompliancePercent: overallPercent,
            OverallStatusText: statusText,
            OverallColorHex: colorHex,
            Exercises: list);
    }

    public static ExercisePlanFactAnalysis AnalyzeExercise(SessionExerciseDetail ex)
    {
        bool hasPlan = ex.PlannedSets.HasValue && ex.PlannedSets.Value > 0 &&
                       ex.PlannedReps.HasValue && ex.PlannedReps.Value > 0;

        int actualSets = ex.Sets.Count;
        int actualRepsTotal = ex.Sets.Sum(s => s.Reps);
        double actualMaxWeight = ex.Sets.Count > 0 ? ex.Sets.Max(s => s.Weight) : 0;
        double actualAvgWeight = ex.Sets.Count > 0 ? Math.Round(ex.Sets.Average(s => s.Weight), 1) : 0;
        double actualVolume = ex.Sets.Sum(s => s.Weight * s.Reps);

        var rpeSets = ex.Sets.Where(s => s.Rpe.HasValue).Select(s => s.Rpe!.Value).ToList();
        double? actualAvgRpe = rpeSets.Count > 0 ? Math.Round(rpeSets.Average(), 1) : null;

        if (!hasPlan)
        {
            return new ExercisePlanFactAnalysis(
                ExerciseId: ex.ExerciseId,
                ExerciseName: ex.ExerciseName,
                HasPlan: false,
                PlanText: "Без плана",
                PlannedSets: 0,
                ActualSets: actualSets,
                PlannedRepsTotal: 0,
                ActualRepsTotal: actualRepsTotal,
                PlannedWeight: null,
                ActualMaxWeight: actualMaxWeight,
                ActualAvgWeight: actualAvgWeight,
                PlannedVolume: null,
                ActualVolume: actualVolume,
                PlannedRpe: null,
                ActualAvgRpe: actualAvgRpe,
                SetsCompliancePercent: 100,
                RepsCompliancePercent: 100,
                OverallCompliancePercent: 100,
                StatusBadgeText: "Без плана",
                ComplianceColorHex: "#94A3B8");
        }

        int plannedSets = ex.PlannedSets!.Value;
        int plannedRepsPerSet = ex.PlannedReps!.Value;
        int plannedRepsTotal = plannedSets * plannedRepsPerSet;
        double? plannedWeight = ex.PlannedWeight;
        double? plannedVolume = plannedWeight.HasValue ? plannedSets * plannedRepsPerSet * plannedWeight.Value : null;

        int setsPercent = (int)Math.Round(Math.Min(1.0, (double)actualSets / plannedSets) * 100);
        int repsPercent = plannedRepsTotal > 0
            ? (int)Math.Round(Math.Min(1.0, (double)actualRepsTotal / plannedRepsTotal) * 100)
            : 100;

        double overallFraction;
        if (plannedWeight.HasValue && plannedWeight.Value > 0)
        {
            double weightScore = Math.Min(1.0, actualAvgWeight / plannedWeight.Value);
            overallFraction = (setsPercent / 100.0) * 0.4 + (repsPercent / 100.0) * 0.4 + weightScore * 0.2;
        }
        else
        {
            overallFraction = (setsPercent / 100.0) * 0.5 + (repsPercent / 100.0) * 0.5;
        }

        int overallPercent = (int)Math.Round(overallFraction * 100);

        string badgeText;
        string colorHex;

        if (overallPercent >= 100)
        {
            badgeText = "Выполнено 100%";
            colorHex = "#10B981"; // Emerald
        }
        else if (overallPercent >= 80)
        {
            badgeText = $"{overallPercent}%";
            colorHex = "#CCFF00"; // Volt
        }
        else if (overallPercent >= 50)
        {
            badgeText = $"{overallPercent}%";
            colorHex = "#EAB308"; // Amber
        }
        else
        {
            badgeText = $"{overallPercent}%";
            colorHex = "#EF4444"; // Crimson
        }

        return new ExercisePlanFactAnalysis(
            ExerciseId: ex.ExerciseId,
            ExerciseName: ex.ExerciseName,
            HasPlan: true,
            PlanText: ex.PlanText,
            PlannedSets: plannedSets,
            ActualSets: actualSets,
            PlannedRepsTotal: plannedRepsTotal,
            ActualRepsTotal: actualRepsTotal,
            PlannedWeight: plannedWeight,
            ActualMaxWeight: actualMaxWeight,
            ActualAvgWeight: actualAvgWeight,
            PlannedVolume: plannedVolume,
            ActualVolume: actualVolume,
            PlannedRpe: ex.PlannedRpe,
            ActualAvgRpe: actualAvgRpe,
            SetsCompliancePercent: setsPercent,
            RepsCompliancePercent: repsPercent,
            OverallCompliancePercent: overallPercent,
            StatusBadgeText: badgeText,
            ComplianceColorHex: colorHex);
    }
}
