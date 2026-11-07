using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Myonic.Core.Analysis;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Core.Services;

namespace Myonic.Data.Services;

public class GeminiAiAnalysisService : IAiAnalysisService, IDisposable
{
    private readonly IAiSettingsService _settingsService;
    private readonly IWorkoutSessionRepository _sessionRepository;
    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiAiAnalysisService(
        IAiSettingsService settingsService,
        IWorkoutSessionRepository sessionRepository,
        HttpClient? httpClient = null)
    {
        _settingsService = settingsService;
        _sessionRepository = sessionRepository;

        if (httpClient != null)
        {
            _httpClient = httpClient;
            _disposeClient = false;
        }
        else
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(25)
            };
            _disposeClient = true;
        }
    }

    public void Dispose()
    {
        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
    }

    public async Task<AiConnectionTestResult> TestConnectionAsync(string apiKey, string modelName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AiConnectionTestResult.Failure("API-ключ не указан. Пожалуйста, введите ваш ключ Google Gemini.");
        }

        var model = string.IsNullOrWhiteSpace(modelName) ? "gemini-2.5-flash" : modelName.Trim();

        try
        {
            var requestBody = new GeminiRequest
            {
                Contents =
                [
                    new GeminiContent
                    {
                        Parts = [new GeminiPart { Text = "Ответь одним словом: 'Подключено'" }]
                    }
                ],
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = 0.2,
                    MaxOutputTokens = 30
                }
            };

            var (isSuccess, text, error) = await ExecuteGeminiCallAsync(apiKey.Trim(), model, requestBody, cancellationToken);
            if (!isSuccess)
            {
                return AiConnectionTestResult.Failure(error ?? "Ошибка соединения с Gemini API.");
            }

            return AiConnectionTestResult.Success($"Связь с моделью {model} успешно проверена!");
        }
        catch (TaskCanceledException)
        {
            return AiConnectionTestResult.Failure("Превышено время ожидания ответа (тайм-аут). Проверьте интернет.");
        }
        catch (HttpRequestException ex)
        {
            return AiConnectionTestResult.Failure($"Ошибка сети при подключении к Google: {ex.Message}");
        }
        catch (Exception ex)
        {
            return AiConnectionTestResult.Failure($"Непредвиденная ошибка: {ex.Message}");
        }
    }

    public async Task<AiAnalysisResult> AnalyzeWorkoutSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var apiKey = _settingsService.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AiAnalysisResult.Failure("API-ключ не задан в Настройках приложения. Перейдите в Настройки и сохраните ваш Google Gemini API ключ.");
        }

        var detail = await _sessionRepository.GetSessionDetailAsync(sessionId);
        if (detail == null)
        {
            return AiAnalysisResult.Failure($"Тренировка с ID {sessionId} не найдена в базе данных.");
        }

        var planFact = PlanFactAnalyzer.Analyze(detail);
        var prompt = AiPromptBuilder.BuildWorkoutAnalysisPrompt(detail, planFact);
        var model = _settingsService.GetSelectedModel();

        return await GenerateAnalysisAsync(apiKey, model, prompt, cancellationToken);
    }

    public async Task<AiAnalysisResult> AnalyzeExerciseProgressAsync(int exerciseId, int days = 90, CancellationToken cancellationToken = default)
    {
        var apiKey = _settingsService.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AiAnalysisResult.Failure("API-ключ не задан в Настройках приложения. Перейдите в Настройки и сохраните ваш Google Gemini API ключ.");
        }

        var history = await _sessionRepository.GetExerciseHistoryAsync(exerciseId);
        if (history == null || string.IsNullOrWhiteSpace(history.ExerciseName))
        {
            return AiAnalysisResult.Failure($"Упражнение с ID {exerciseId} не найдено.");
        }

        var prompt = AiPromptBuilder.BuildExerciseProgressPrompt(history, days);
        var model = _settingsService.GetSelectedModel();

        return await GenerateAnalysisAsync(apiKey, model, prompt, cancellationToken);
    }

    private async Task<AiAnalysisResult> GenerateAnalysisAsync(string apiKey, string model, string prompt, CancellationToken cancellationToken)
    {
        var chosenModel = string.IsNullOrWhiteSpace(model) ? "gemini-2.5-flash" : model.Trim();

        try
        {
            var requestBody = new GeminiRequest
            {
                Contents =
                [
                    new GeminiContent
                    {
                        Parts = [new GeminiPart { Text = prompt }]
                    }
                ],
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = 0.7,
                    MaxOutputTokens = 2048
                }
            };

            var (isSuccess, text, error) = await ExecuteGeminiCallAsync(apiKey.Trim(), chosenModel, requestBody, cancellationToken);
            if (!isSuccess || string.IsNullOrWhiteSpace(text))
            {
                return AiAnalysisResult.Failure(error ?? "Не удалось получить ответ от ИИ-тренера.");
            }

            return AiAnalysisResult.Success(text.Trim(), chosenModel);
        }
        catch (TaskCanceledException)
        {
            return AiAnalysisResult.Failure("Превышено время ожидания ответа (тайм-аут). Проверьте интернет-соединение.");
        }
        catch (HttpRequestException ex)
        {
            return AiAnalysisResult.Failure($"Сетевая ошибка при запросе к Gemini: {ex.Message}");
        }
        catch (Exception ex)
        {
            return AiAnalysisResult.Failure($"Ошибка генерации анализа: {ex.Message}");
        }
    }

    private async Task<(bool IsSuccess, string? Text, string? Error)> ExecuteGeminiCallAsync(
        string apiKey,
        string model,
        GeminiRequest requestBody,
        CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        var jsonPayload = JsonSerializer.Serialize(requestBody, JsonOptions);
        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = ParseApiError(response.StatusCode, responseString);
            return (false, null, errorMessage);
        }

        try
        {
            var result = JsonSerializer.Deserialize<GeminiResponse>(responseString, JsonOptions);
            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                var finishReason = result?.Candidates?.FirstOrDefault()?.FinishReason;
                if (!string.IsNullOrEmpty(finishReason) && finishReason != "STOP")
                {
                    return (false, null, $"Генерация остановлена сервером по причине: {finishReason}");
                }
                return (false, null, "Получен пустой ответ от ИИ модели.");
            }

            return (true, text, null);
        }
        catch (JsonException ex)
        {
            return (false, null, $"Не удалось разобрать ответ от Gemini API: {ex.Message}");
        }
    }

    private static string ParseApiError(HttpStatusCode statusCode, string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.TryGetProperty("message", out var msgElement))
                {
                    var msg = msgElement.GetString();
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        if (statusCode == HttpStatusCode.BadRequest && msg.Contains("API_KEY_INVALID", StringComparison.OrdinalIgnoreCase))
                        {
                            return "Недействительный Google Gemini API-ключ. Проверьте правильность ключа в Настройках.";
                        }
                        if (statusCode == HttpStatusCode.Forbidden)
                        {
                            return $"Доступ к Gemini API запрещён (403): {msg}";
                        }
                        if ((int)statusCode == 429)
                        {
                            return "Превышен лимит запросов к Google Gemini API (429 Too Many Requests). Попробуйте чуть позже.";
                        }
                        return $"Ошибка Gemini API ({statusCode}): {msg}";
                    }
                }
            }
        }
        catch
        {
            // Ignore parse errors, fallback to status code
        }

        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Некорректный запрос к Gemini API (400 Bad Request). Проверьте ключ и выбранную модель.",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Ошибка авторизации Gemini API. Проверьте правильность API ключа в Настройках.",
            (HttpStatusCode)429 => "Превышен лимит запросов к Gemini API (429 Too Many Requests).",
            HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable => "Сервер Gemini временно недоступен. Попробуйте позже.",
            _ => $"Ошибка связи с сервером Gemini: {(int)statusCode} {statusCode}"
        };
    }

    #region Internal DTOs for Gemini REST API
    private sealed class GeminiRequest
    {
        public List<GeminiContent> Contents { get; set; } = [];
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private sealed class GeminiContent
    {
        public List<GeminiPart> Parts { get; set; } = [];
        public string? Role { get; set; }
    }

    private sealed class GeminiPart
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiGenerationConfig
    {
        public double? Temperature { get; set; }
        public int? MaxOutputTokens { get; set; }
    }

    private sealed class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
    }
    #endregion
}
