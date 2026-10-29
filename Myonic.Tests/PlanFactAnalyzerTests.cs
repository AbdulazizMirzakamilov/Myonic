using Myonic.Core.Analysis;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Tests;

public class PlanFactAnalyzerTests
{
    [Fact]
    public void AnalyzeExercise_ExactExecution_Returns100Percent()
    {
        var detail = new SessionExerciseDetail
        {
            ExerciseId = 1,
            ExerciseName = "Жим лёжа",
            PlannedSets = 4,
            PlannedReps = 8,
            PlannedWeight = 80,
            PlanText = "4 × 8, 80 кг",
            Sets =
            [
                new SetEntry { SetNumber = 1, Weight = 80, Reps = 8 },
                new SetEntry { SetNumber = 2, Weight = 80, Reps = 8 },
                new SetEntry { SetNumber = 3, Weight = 80, Reps = 8 },
                new SetEntry { SetNumber = 4, Weight = 80, Reps = 8 }
            ]
        };

        var result = PlanFactAnalyzer.AnalyzeExercise(detail);

        Assert.True(result.HasPlan);
        Assert.Equal(4, result.ActualSets);
        Assert.Equal(32, result.ActualRepsTotal);
        Assert.Equal(100, result.SetsCompliancePercent);
        Assert.Equal(100, result.RepsCompliancePercent);
        Assert.Equal(100, result.OverallCompliancePercent);
        Assert.Equal("Выполнено 100%", result.StatusBadgeText);
    }

    [Fact]
    public void AnalyzeExercise_MissedRepsAndSets_CalculatesLowerPercentage()
    {
        var detail = new SessionExerciseDetail
        {
            ExerciseId = 1,
            ExerciseName = "Жим лёжа",
            PlannedSets = 4,
            PlannedReps = 8,
            PlannedWeight = 80,
            Sets =
            [
                new SetEntry { SetNumber = 1, Weight = 80, Reps = 8 },
                new SetEntry { SetNumber = 2, Weight = 80, Reps = 8 }
                // Only 2 sets done instead of 4
            ]
        };

        var result = PlanFactAnalyzer.AnalyzeExercise(detail);

        Assert.True(result.HasPlan);
        Assert.Equal(2, result.ActualSets);
        Assert.Equal(16, result.ActualRepsTotal);
        Assert.Equal(50, result.SetsCompliancePercent);
        Assert.Equal(50, result.RepsCompliancePercent);
        // (0.5 * 0.4) + (0.5 * 0.4) + (1.0 * 0.2) = 0.2 + 0.2 + 0.2 = 0.6 => 60%
        Assert.Equal(60, result.OverallCompliancePercent);
    }

    [Fact]
    public void Analyze_WorkoutSummary_CalculatesOverallCompliance()
    {
        var session = new WorkoutSessionDetail
        {
            Id = 1,
            Title = "Грудь + Спина",
            Exercises =
            [
                new SessionExerciseDetail
                {
                    ExerciseId = 1,
                    ExerciseName = "Жим лёжа",
                    PlannedSets = 4,
                    PlannedReps = 8,
                    PlannedWeight = 80,
                    Sets =
                    [
                        new SetEntry { Weight = 80, Reps = 8 },
                        new SetEntry { Weight = 80, Reps = 8 },
                        new SetEntry { Weight = 80, Reps = 8 },
                        new SetEntry { Weight = 80, Reps = 8 }
                    ]
                },
                new SessionExerciseDetail
                {
                    ExerciseId = 2,
                    ExerciseName = "Подтягивания",
                    PlannedSets = 4,
                    PlannedReps = 10,
                    Sets =
                    [
                        new SetEntry { Weight = 0, Reps = 10 },
                        new SetEntry { Weight = 0, Reps = 10 },
                        new SetEntry { Weight = 0, Reps = 10 },
                        new SetEntry { Weight = 0, Reps = 10 }
                    ]
                }
            ]
        };

        var summary = PlanFactAnalyzer.Analyze(session);

        Assert.True(summary.HasPlan);
        Assert.Equal(2, summary.TotalExercises);
        Assert.Equal(8, summary.TotalPlannedSets);
        Assert.Equal(8, summary.TotalActualSets);
        Assert.Equal(100, summary.OverallCompliancePercent);
    }
}
