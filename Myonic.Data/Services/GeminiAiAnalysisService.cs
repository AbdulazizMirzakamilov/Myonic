using System.Net;
using System.Net.Http.Headers;
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
                Timeout = TimeSpan.FromSeconds(30)
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

    public Task<AiConnectionTestResult> TestConnectionAsync(string apiKey, string modelName, CancellationToken cancellationToken = default)
    {
        return TestConnectionAsync(_settingsService.Provider, apiKey, modelName, _settingsService.GetBaseUrl(), cancellationToken);
    }

    public async Task<AiConnectionTestResult> TestConnectionAsync(
        AiProviderType provider,
        string apiKey,
        string modelName,
        string? baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AiConnectionTestResult.Failure("API-ключ не указан. Пожалуйста, введите ваш API-ключ.");
        }

        var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? AiProviderDefaults.GetDefaultBaseUrl(provider)
            : baseUrl.Trim().TrimEnd('/');

        var model = string.IsNullOrWhiteSpace(modelName)
            ? (AiProviderDefaults.GetDefaultModels(provider).FirstOrDefault() ?? "default")
            : modelName.Trim();

        try
        {
            if (provider == AiProviderType.Gemini)
            {
                var requestBody = new GeminiRequest
                {
                    Contents =
                    [
                        new GeminiContent
                        {
                            Parts = [new GeminiPart { Text = "Ответь одним словом: Готов" }]
                        }
                    ],
                    GenerationConfig = new GeminiGenerationConfig
                    {
                        Temperature = 0.2,
                        MaxOutputTokens = 30
                    }
                };

                var (isSuccess, text, error) = await ExecuteGeminiCallAsync(apiKey.Trim(), model, effectiveBaseUrl, requestBody, cancellationToken);
                if (!isSuccess)
                {
                    return AiConnectionTestResult.Failure(error ?? "Ошибка соединения с Gemini API.");
                }

                return AiConnectionTestResult.Success($"Связь с моделью {model} успешно проверена!");
            }
            else
            {
                // OpenAI-compatible Chat Completions
                var requestBody = new OpenAiChatRequest
                {
                    Model = model,
                    Messages =
                    [
                        new OpenAiChatMessage { Role = "user", Content = "Ответь одним словом: Готов" }
                    ],
                    Temperature = 0.2,
                    MaxTokens = 30
                };

                var (isSuccess, text, error) = await ExecuteOpenAiCompatibleCallAsync(apiKey.Trim(), effectiveBaseUrl, requestBody, cancellationToken);
                if (!isSuccess)
                {
                    return AiConnectionTestResult.Failure(error ?? $"Ошибка соединения с {AiProviderDefaults.GetDisplayName(provider)}.");
                }

                return AiConnectionTestResult.Success($"Связь с {AiProviderDefaults.GetDisplayName(provider)} ({model}) успешно проверена!");
            }
        }
        catch (TaskCanceledException)
        {
            return AiConnectionTestResult.Failure("Превышено время ожидания ответа (тайм-аут). Проверьте интернет-соединение.");
        }
        catch (HttpRequestException ex)
        {
            return AiConnectionTestResult.Failure($"Ошибка сети: {ex.Message}");
        }
        catch (Exception ex)
        {
            return AiConnectionTestResult.Failure($"Непредвиденная ошибка: {ex.Message}");
        }
    }

    public async Task<AiAnalysisResult> AnalyzeWorkoutSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var apiKey = await _settingsService.GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AiAnalysisResult.Failure("API-ключ не задан в Настройках приложения. Перейдите в Настройки и сохраните ваш API-ключ.");
        }

        var detail = await _sessionRepository.GetSessionDetailAsync(sessionId);
        if (detail == null)
        {
            return AiAnalysisResult.Failure($"Тренировка с ID {sessionId} не найдена в базе данных.");
        }

        var planFact = PlanFactAnalyzer.Analyze(detail);
        var prompt = AiPromptBuilder.BuildWorkoutAnalysisPrompt(detail, planFact);
        var provider = _settingsService.Provider;
        var model = _settingsService.GetSelectedModel();
        var baseUrl = _settingsService.GetBaseUrl();

        return await GenerateAnalysisAsync(provider, apiKey, model, baseUrl, prompt, cancellationToken);
    }

    public async Task<AiAnalysisResult> AnalyzeExerciseProgressAsync(int exerciseId, int days = 90, CancellationToken cancellationToken = default)
    {
        var apiKey = await _settingsService.GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AiAnalysisResult.Failure("API-ключ не задан в Настройках приложения. Перейдите в Настройки и сохраните ваш API-ключ.");
        }

        var history = await _sessionRepository.GetExerciseHistoryAsync(exerciseId);
        if (history == null || string.IsNullOrWhiteSpace(history.ExerciseName))
        {
            return AiAnalysisResult.Failure($"Упражнение с ID {exerciseId} не найдено.");
        }

        var prompt = AiPromptBuilder.BuildExerciseProgressPrompt(history, days);
        var provider = _settingsService.Provider;
        var model = _settingsService.GetSelectedModel();
        var baseUrl = _settingsService.GetBaseUrl();

        return await GenerateAnalysisAsync(provider, apiKey, model, baseUrl, prompt, cancellationToken);
    }

    private async Task<AiAnalysisResult> GenerateAnalysisAsync(
        AiProviderType provider,
        string apiKey,
        string model,
        string baseUrl,
        string prompt,
        CancellationToken cancellationToken)
    {
        var chosenModel = string.IsNullOrWhiteSpace(model)
            ? (AiProviderDefaults.GetDefaultModels(provider).FirstOrDefault() ?? "default")
            : model.Trim();

        var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? AiProviderDefaults.GetDefaultBaseUrl(provider)
            : baseUrl.Trim().TrimEnd('/');

        try
        {
            if (provider == AiProviderType.Gemini)
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

                var (isSuccess, text, error) = await ExecuteGeminiCallAsync(apiKey.Trim(), chosenModel, effectiveBaseUrl, requestBody, cancellationToken);
                if (!isSuccess || string.IsNullOrWhiteSpace(text))
                {
                    return AiAnalysisResult.Failure(error ?? "Не удалось сформировать аналитическую сводку.");
                }

                return AiAnalysisResult.Success(text.Trim(), chosenModel);
            }
            else
            {
                var requestBody = new OpenAiChatRequest
                {
                    Model = chosenModel,
                    Messages =
                    [
                        new OpenAiChatMessage { Role = "user", Content = prompt }
                    ],
                    Temperature = 0.7,
                    MaxTokens = 2048
                };

                var (isSuccess, text, error) = await ExecuteOpenAiCompatibleCallAsync(apiKey.Trim(), effectiveBaseUrl, requestBody, cancellationToken);
                if (!isSuccess || string.IsNullOrWhiteSpace(text))
                {
                    return AiAnalysisResult.Failure(error ?? "Не удалось сформировать аналитическую сводку.");
                }

                return AiAnalysisResult.Success(text.Trim(), chosenModel);
            }
        }
        catch (TaskCanceledException)
        {
            return AiAnalysisResult.Failure("Превышено время ожидания ответа (тайм-аут). Проверьте интернет-соединение.");
        }
        catch (HttpRequestException ex)
        {
            var msg = ex.Message;
            if (msg.Contains("key="))
            {
                msg = System.Text.RegularExpressions.Regex.Replace(msg, @"key=[^&\s]+", "key=***");
            }
            return AiAnalysisResult.Failure($"Сетевая ошибка при запросе к ИИ: {msg}");
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (msg.Contains("key="))
            {
                msg = System.Text.RegularExpressions.Regex.Replace(msg, @"key=[^&\s]+", "key=***");
            }
            return AiAnalysisResult.Failure($"Ошибка формирования сводки: {msg}");
        }
    }

    private async Task<(bool IsSuccess, string? Text, string? Error)> ExecuteGeminiCallAsync(
        string apiKey,
        string model,
        string baseUrl,
        GeminiRequest requestBody,
        CancellationToken cancellationToken)
    {
        var cleanBaseUrl = baseUrl.TrimEnd('/');
        var url = $"{cleanBaseUrl}/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        var jsonPayload = JsonSerializer.Serialize(requestBody, JsonOptions);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Headers.Add("x-goog-api-key", apiKey);
        requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
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

    private async Task<(bool IsSuccess, string? Text, string? Error)> ExecuteOpenAiCompatibleCallAsync(
        string apiKey,
        string baseUrl,
        OpenAiChatRequest requestBody,
        CancellationToken cancellationToken)
    {
        var cleanBaseUrl = baseUrl.TrimEnd('/');
        var endpoint = cleanBaseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? cleanBaseUrl
            : $"{cleanBaseUrl}/chat/completions";

        var jsonPayload = JsonSerializer.Serialize(requestBody, JsonOptions);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = ParseApiError(response.StatusCode, responseString);
            return (false, null, errorMessage);
        }

        try
        {
            var result = JsonSerializer.Deserialize<OpenAiChatResponse>(responseString, JsonOptions);
            var text = result?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, null, "Получен пустой ответ от языковой модели.");
            }

            return (true, text, null);
        }
        catch (JsonException ex)
        {
            return (false, null, $"Не удалось разобрать ответ модели: {ex.Message}");
        }
    }

    private static string ParseApiError(HttpStatusCode statusCode, string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                string? msg = null;
                if (errorElement.ValueKind == JsonValueKind.String)
                {
                    msg = errorElement.GetString();
                }
                else if (errorElement.TryGetProperty("message", out var msgProp))
                {
                    msg = msgProp.GetString();
                }

                if (!string.IsNullOrWhiteSpace(msg))
                {
                    if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden || msg.Contains("API_KEY_INVALID", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"Недействительный API-ключ или ошибка доступа ({statusCode}): {msg}";
                    }
                    if ((int)statusCode == 429)
                    {
                        return "Превышен лимит запросов к API (429 Too Many Requests). Проверьте баланс/квоту или попробуйте позже.";
                    }
                    return $"Ошибка API ({statusCode}): {msg}";
                }
            }
        }
        catch
        {
            // Fallback
        }

        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Некорректный запрос к API (400 Bad Request). Проверьте модель и параметры.",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Ошибка авторизации (401/403). Проверьте правильность API ключа в Настройках.",
            (HttpStatusCode)429 => "Превышен лимит запросов (429 Too Many Requests).",
            HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable => "Сервер ИИ-провайдера временно недоступен. Попробуйте позже.",
            _ => $"Ошибка соединения с сервером: {(int)statusCode} {statusCode}"
        };
    }

    #region Internal DTOs for Gemini & OpenAI APIs
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

    private sealed class OpenAiChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAiChatMessage> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }
    }

    private sealed class OpenAiChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OpenAiChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiChatMessage? Message { get; set; }
    }
    #endregion
}
