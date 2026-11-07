using Myonic.Core.Analysis;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Tests;

public class AiPromptBuilderTests
{
    [Fact]
    public void BuildWorkoutAnalysisPrompt_WithExercisesAndSets_ProducesStructuredPrompt()
    {
        var session = new WorkoutSessionDetail
        {
            Id = 42,
            Title = "День груди и трицепса",
            StartedAt = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc),
            FinishedAt = new DateTime(2026, 11, 8, 11, 15, 0, DateTimeKind.Utc),
            Comment = "Отличная тренировка, чувствовал прилив сил",
            Exercises =
            [
                new SessionExerciseDetail
                {
                    ExerciseId = 1,
                    ExerciseName = "Жим штанги лежа",
                    ExerciseType = ExerciseType.Compound,
                    PlanText = "3 × 8–10 @ RPE 8.0",
                    Sets =
                    [
                        new SetEntry { SetNumber = 1, Weight = 100, Reps = 10, Rpe = 8.0 },
                        new SetEntry { SetNumber = 2, Weight = 102.5, Reps = 8, Rpe = 8.5 },
                        new SetEntry { SetNumber = 3, Weight = 105, Reps = 6, Rpe = 9.5, Comment = "Тяжело пошло на 6-м" }
                    ]
                }
            ]
        };

        var planFact = PlanFactAnalyzer.Analyze(session);
        var prompt = AiPromptBuilder.BuildWorkoutAnalysisPrompt(session, planFact);

        Assert.Contains("Жим штанги лежа", prompt);
        Assert.Contains("День груди и трицепса", prompt);
        Assert.Contains("100 кг × 10 повт., RPE 8", prompt);
        Assert.Contains("105 кг × 6 повт., RPE 9.5 [заметка: Тяжело пошло на 6-м]", prompt);
        Assert.Contains("Отличная тренировка", prompt);
        Assert.Contains("75 минут", prompt);
        Assert.Contains("Требования к анализу тренера", prompt);
    }

    [Fact]
    public void BuildWorkoutAnalysisPrompt_EmptySession_HandlesGracefully()
    {
        var session = new WorkoutSessionDetail
        {
            Id = 1,
            Title = "Пустая тренировка",
            StartedAt = DateTime.UtcNow,
            Exercises = []
        };

        var prompt = AiPromptBuilder.BuildWorkoutAnalysisPrompt(session);

        Assert.Contains("Пустая тренировка", prompt);
        Assert.Contains("не было зафиксировано ни одного упражнения", prompt);
    }

    [Fact]
    public void BuildExerciseProgressPrompt_WithSessionsHistory_ProducesDetailedPrompt()
    {
        var history = new ExerciseHistorySummary
        {
            ExerciseId = 10,
            ExerciseName = "Приседания со штангой",
            ExerciseType = ExerciseType.Compound,
            Description = "Базовое упражнение для ног",
            AllTimeMaxWeight = 160,
            AllTimeBest1Rm = 185.5,
            TotalSessionsCount = 5,
            TotalSetsCount = 20,
            Sessions =
            [
                new ExerciseHistoryItem
                {
                    SessionId = 101,
                    WorkoutTitle = "Ноги А",
                    Date = new DateTime(2026, 10, 1),
                    MaxWeight = 140,
                    Best1Rm = 162.4,
                    TotalVolume = 4200,
                    Sets =
                    [
                        new ExerciseHistorySetItem { SetNumber = 1, Weight = 140, Reps = 5, Rpe = 8 }
                    ]
                },
                new ExerciseHistoryItem
                {
                    SessionId = 102,
                    WorkoutTitle = "Ноги Б",
                    Date = new DateTime(2026, 11, 1),
                    MaxWeight = 150,
                    Best1Rm = 174.0,
                    TotalVolume = 4500,
                    Sets =
                    [
                        new ExerciseHistorySetItem { SetNumber = 1, Weight = 150, Reps = 5, Rpe = 8.5 }
                    ]
                }
            ]
        };

        var prompt = AiPromptBuilder.BuildExerciseProgressPrompt(history, 90);

        Assert.Contains("Приседания со штангой", prompt);
        Assert.Contains("160 кг", prompt);
        Assert.Contains("185.5 кг", prompt);
        Assert.Contains("Ноги А", prompt);
        Assert.Contains("Ноги Б", prompt);
        Assert.Contains("140×5 @8", prompt);
        Assert.Contains("150×5 @8.5", prompt);
        Assert.Contains("Стратегия прогрессии", prompt);
    }

    [Fact]
    public void BuildExerciseProgressPrompt_EmptyHistory_HandlesGracefully()
    {
        var history = new ExerciseHistorySummary
        {
            ExerciseId = 15,
            ExerciseName = "Подтягивания",
            ExerciseType = ExerciseType.Compound,
            Sessions = []
        };

        var prompt = AiPromptBuilder.BuildExerciseProgressPrompt(history);

        Assert.Contains("Подтягивания", prompt);
        Assert.Contains("История выполнения данного упражнения отсутствует", prompt);
    }
}
