using System.Globalization;
using System.Text;
using Myonic.Core.Analysis;
using Myonic.Core.Repositories;

namespace Myonic.Core.Models;

public enum AiProviderType
{
    Gemini = 0,
    OpenAI = 1,
    DeepSeek = 2,
    Groq = 3,
    Qwen = 4,
    Mistral = 5,
    Nvidia = 6,
    Kimi = 7,
    Zhipu = 8,
    Custom = 9
}

public static class AiProviderDefaults
{
    public static string GetDisplayName(AiProviderType provider) => provider switch
    {
        AiProviderType.Gemini => "Google Gemini",
        AiProviderType.OpenAI => "OpenAI (ChatGPT)",
        AiProviderType.DeepSeek => "DeepSeek",
        AiProviderType.Groq => "Groq (Fast Llama)",
        AiProviderType.Qwen => "Qwen (Alibaba DashScope)",
        AiProviderType.Mistral => "Mistral AI",
        AiProviderType.Nvidia => "NVIDIA NIM",
        AiProviderType.Kimi => "Moonshot Kimi",
        AiProviderType.Zhipu => "Zhipu AI (GLM)",
        AiProviderType.Custom => "Пользовательский (OpenAI-совместимый)",
        _ => provider.ToString()
    };

    public static string GetDefaultBaseUrl(AiProviderType provider) => provider switch
    {
        AiProviderType.Gemini => "https://generativelanguage.googleapis.com",
        AiProviderType.OpenAI => "https://api.openai.com/v1",
        AiProviderType.DeepSeek => "https://api.deepseek.com/v1",
        AiProviderType.Groq => "https://api.groq.com/openai/v1",
        AiProviderType.Qwen => "https://dashscope-intl.aliyuncs.com/compatible-mode/v1",
        AiProviderType.Mistral => "https://api.mistral.ai/v1",
        AiProviderType.Nvidia => "https://integrate.api.nvidia.com/v1",
        AiProviderType.Kimi => "https://api.moonshot.cn/v1",
        AiProviderType.Zhipu => "https://open.bigmodel.cn/api/paas/v4",
        AiProviderType.Custom => "http://localhost:11434/v1",
        _ => "https://api.openai.com/v1"
    };

    public static IReadOnlyList<string> GetDefaultModels(AiProviderType provider) => provider switch
    {
        AiProviderType.Gemini => ["gemini-2.5-flash", "gemini-2.5-pro", "gemini-2.0-flash", "gemini-4-argon", "gemini-1.5-flash", "gemini-1.5-pro"],
        AiProviderType.OpenAI => ["gpt-4o", "gpt-4o-mini", "o3-mini", "o1", "gpt-4-turbo"],
        AiProviderType.DeepSeek => ["deepseek-chat", "deepseek-reasoner"],
        AiProviderType.Groq => ["llama-3.3-70b-versatile", "llama-3.1-8b-instant", "mixtral-8x7b-32768"],
        AiProviderType.Qwen => ["qwen-plus", "qwen-turbo", "qwen-max", "qwen2.5-72b-instruct"],
        AiProviderType.Mistral => ["mistral-large-latest", "mistral-small-latest", "codestral-latest"],
        AiProviderType.Nvidia => ["meta/llama-3.3-70b-instruct", "deepseek-ai/deepseek-r1"],
        AiProviderType.Kimi => ["moonshot-v1-8k", "moonshot-v1-32k", "moonshot-v1-128k"],
        AiProviderType.Zhipu => ["glm-4-plus", "glm-4-flash", "glm-4-air"],
        AiProviderType.Custom => ["custom-model"],
        _ => ["gpt-4o-mini"]
    };
}

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
        var inv = CultureInfo.InvariantCulture;

        sb.AppendLine("Ты — система аналитики и сводки силовых тренировок.");
        sb.AppendLine("Твоя задача — подготовить структурированную, объективную сводку и разбор завершённой тренировки атлета.");
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
        sb.AppendLine("### Требования к аналитической сводке:");
        sb.AppendLine("1. **Общая сводка:** Краткое резюме тренировки (объём, интенсивность, баланс).");
        sb.AppendLine("2. **Анализ нагрузки и RPE:** Оценка распределения усилий, выявление признаков перегруза (RPE >= 9.5) или недогруза.");
        sb.AppendLine("3. **Выполнение плана:** Если был план, оценка точности соблюдения.");
        sb.AppendLine("4. **Аналитические выводы:** 2-3 конкретных вывода на следующую сессию (коррекция объёма, интенсивности или восстановление).");
        sb.AppendLine("Форматируй лаконично, структурированно, опираясь исключительно на факты.");

        return sb.ToString();
    }

    public static string BuildExerciseProgressPrompt(ExerciseHistorySummary history, int days = 90)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;

        sb.AppendLine("Ты — система аналитики прогресса в силовых упражнениях.");
        sb.AppendLine("Твоя задача — проанализировать динамику показателей атлета в конкретном упражнении и подготовить объективную сводку.");
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
        sb.AppendLine("### Требования к аналитической сводке:");
        sb.AppendLine("1. **Динамика силы:** Оценка роста рабочего веса и расчетного 1ПМ. Наличие прогресса, спада или плато.");
        sb.AppendLine("2. **Анализ объёма и интенсивности:** Как менялся тоннаж и сложность подходов.");
        sb.AppendLine("3. **Стратегия прогрессии:** Оптимальная схема перегрузки (линейная, волновая, двойная прогрессия).");
        sb.AppendLine("4. **Вспомогательные движения:** Рекомендации по подсобным упражнениям при необходимости.");
        sb.AppendLine("Будь конкретен, опирайся на фактические цифры.");

        return sb.ToString();
    }
}
