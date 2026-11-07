using System.Globalization;
using System.Text;
using Myonic.Core.Analysis;
using Myonic.Core.Repositories;

namespace Myonic.Core.Models;

public sealed class AiAnalysisResult
{
    public bool IsSuccess { get; init; }
    public string? Content { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public string ModelUsed { get; init; } = string.Empty;

    public static AiAnalysisResult Success(string content, string modelUsed) => new()
    {
        IsSuccess = true,
        Content = content,
        ModelUsed = modelUsed,
        GeneratedAt = DateTime.UtcNow
    };

    public static AiAnalysisResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        GeneratedAt = DateTime.UtcNow
    };
}

public sealed class AiConnectionTestResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;

    public static AiConnectionTestResult Success(string message = "Подключение успешно установлено!") => new()
    {
        IsSuccess = true,
        Message = message
    };

    public static AiConnectionTestResult Failure(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };
}

public static class AiPromptBuilder
{
    public static string BuildWorkoutAnalysisPrompt(WorkoutSessionDetail session, WorkoutPlanFactSummary? planFact = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Ты — профессиональный тренер по силовому тренингу и пауэрлифтингу.");
        sb.AppendLine("Твоя задача — провести детальный, объективный и поддерживающий разбор завершённой тренировки атлета.");
        sb.AppendLine("Отвечай строго на русском языке, используя структурированный Markdown (заголовки ##, маркеры, жирный шрифт).");
        sb.AppendLine();
        sb.AppendLine("### Данные тренировки:");
        sb.AppendLine($"- Название: {(string.IsNullOrWhiteSpace(session.Title) ? "Тренировка" : session.Title)}");
        sb.AppendLine($"- Дата начала: {session.StartedAt:dd.MM.yyyy HH:mm}");
        if (session.FinishedAt.HasValue)
        {
            var duration = session.FinishedAt.Value - session.StartedAt;
            sb.AppendLine($"- Длительность: {(int)duration.TotalMinutes} минут");
        }
        if (!string.IsNullOrWhiteSpace(session.Comment))
        {
            sb.AppendLine($"- Комментарий атлета: {session.Comment}");
        }

        var inv = CultureInfo.InvariantCulture;

        if (planFact != null && planFact.HasPlan)
        {
            sb.AppendLine($"- Выполнение плана: {planFact.OverallCompliancePercent}% ({planFact.OverallStatusText})");
            sb.AppendLine($"- Сеты: {planFact.TotalActualSets} из {planFact.TotalPlannedSets} план.");
            sb.AppendLine($"- Повторения: {planFact.TotalActualReps} из {planFact.TotalPlannedReps} план.");
            sb.AppendLine($"- Тоннаж (объём): {planFact.TotalActualVolume.ToString("N0", inv)} кг (план: {planFact.TotalPlannedVolume.ToString("N0", inv)} кг)");
        }

        sb.AppendLine();
        sb.AppendLine("### Выполненные упражнения и сеты:");

        if (session.Exercises.Count == 0)
        {
            sb.AppendLine("В этой тренировке не было зафиксировано ни одного упражнения.");
        }
        else
        {
            int exIndex = 1;
            foreach (var ex in session.Exercises)
            {
                sb.AppendLine($"{exIndex}. {ex.ExerciseName} ({ex.ExerciseType}):");
                if (!string.IsNullOrWhiteSpace(ex.PlanText))
                {
                    sb.AppendLine($"   План: {ex.PlanText}");
                }

                if (ex.Sets.Count == 0)
                {
                    sb.AppendLine("   - Подходы не записаны.");
                }
                else
                {
                    foreach (var set in ex.Sets)
                    {
                        var rpeStr = set.Rpe.HasValue ? $", RPE {set.Rpe.Value.ToString("0.#", inv)}" : string.Empty;
                        var commentStr = !string.IsNullOrWhiteSpace(set.Comment) ? $" [заметка: {set.Comment}]" : string.Empty;
                        sb.AppendLine($"   - Сет {set.SetNumber}: {set.Weight.ToString(inv)} кг × {set.Reps} повт.{rpeStr}{commentStr}");
                    }
                }
                exIndex++;
            }
        }

        sb.AppendLine();
        sb.AppendLine("### Требования к анализу тренера:");
        sb.AppendLine("1. **Общая оценка:** Краткое резюме тренировки (объём, интенсивность, баланс).");
        sb.AppendLine("2. **Анализ нагрузки и RPE:** Оцени распределение усилий. Были ли признаки перегруза (RPE >= 9.5) или преждевременного утомления?");
        sb.AppendLine("3. **Выполнение плана:** Если был план, оцени точность его соблюдения.");
        sb.AppendLine("4. **Рекомендации:** 2-3 конкретных совета на следующую тренировку (восстановление, коррекция весов или повторений).");
        sb.AppendLine("Будь лаконичен, профессионален и избегай банальных общих фраз.");

        return sb.ToString();
    }

    public static string BuildExerciseProgressPrompt(ExerciseHistorySummary history, int days = 90)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;

        sb.AppendLine("Ты — профессиональный тренер по силовому тренингу и спортивной науке.");
        sb.AppendLine("Твоя задача — проанализировать динамику прогресса атлета в конкретном упражнении и дать рекомендации по дальнейшей прогрессии нагрузок.");
        sb.AppendLine("Отвечай строго на русском языке, используя структурированный Markdown.");
        sb.AppendLine();
        sb.AppendLine($"### Упражнение: {history.ExerciseName}");
        sb.AppendLine($"- Тип: {history.ExerciseType}");
        if (!string.IsNullOrWhiteSpace(history.Description))
        {
            sb.AppendLine($"- Описание: {history.Description}");
        }
        sb.AppendLine($"- Рекордный вес за всё время: {history.AllTimeMaxWeight.ToString(inv)} кг");
        sb.AppendLine($"- Лучший расчётный 1ПМ (1RM): {history.AllTimeBest1Rm.ToString("0.#", inv)} кг");
        sb.AppendLine($"- Всего зафиксировано сессий: {history.TotalSessionsCount}, подходов: {history.TotalSetsCount}");
        sb.AppendLine();

        sb.AppendLine($"### История тренировок (последние записи, горизонт {days} дней):");

        var sessions = history.Sessions
            .OrderBy(s => s.Date)
            .ToList();

        if (sessions.Count == 0)
        {
            sb.AppendLine("История выполнения данного упражнения отсутствует.");
        }
        else
        {
            foreach (var session in sessions)
            {
                sb.AppendLine($"- **{session.Date:dd.MM.yyyy}** ({session.WorkoutTitle}):");
                sb.AppendLine($"  Макс вес: {session.MaxWeight.ToString(inv)} кг, Расч. 1ПМ: {session.Best1Rm.ToString("0.#", inv)} кг, Тоннаж: {session.TotalVolume.ToString("N0", inv)} кг");
                var setsStr = string.Join("; ", session.Sets.Select(s =>
                {
                    var rpe = s.Rpe.HasValue ? $" @{s.Rpe.Value.ToString("0.#", inv)}" : "";
                    return $"{s.Weight.ToString(inv)}×{s.Reps}{rpe}";
                }));
                sb.AppendLine($"  Подходы: {setsStr}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("### Требования к анализу прогресса:");
        sb.AppendLine("1. **Динамика силы:** Оцени рост рабочего веса и расчетного 1ПМ. Наблюдается ли стабильный прогресс, спад или плато?");
        sb.AppendLine("2. **Анализ объёма и интенсивности:** Как менялся тоннаж и сложность подходов?");
        sb.AppendLine("3. **Стратегия прогрессии:** Предложи схему перегрузки на следующие 3-4 недели (например: линейная, волновая, двойная прогрессия).");
        sb.AppendLine("4. **Подсобные упражнения:** Порекомендуй 1-2 вспомогательных упражнения для устранения слабых звеньев (если применимо).");
        sb.AppendLine("Будь конкретен, мотивируй атлета и опирайся на фактические цифры.");

        return sb.ToString();
    }
}
