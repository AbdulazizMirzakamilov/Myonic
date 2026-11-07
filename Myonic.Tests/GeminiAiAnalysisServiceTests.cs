using System.Net;
using System.Text;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Core.Services;
using Myonic.Data.Services;

namespace Myonic.Tests;

public class GeminiAiAnalysisServiceTests
{
    private class MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handlerFunc) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return handlerFunc(request);
        }
    }

    private class TestAiSettingsService : IAiSettingsService
    {
        public AiProviderType Provider { get; set; } = AiProviderType.Gemini;
        public string? ApiKey { get; set; } = "test-fake-key-12345";
        public string SelectedModel { get; set; } = "gemini-2.5-flash";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

        public string? GetApiKey() => ApiKey;
        public void SaveApiKey(string? apiKey) => ApiKey = apiKey;
        public string GetSelectedModel() => SelectedModel;
        public void SaveSelectedModel(string modelName) => SelectedModel = modelName;
        public string GetBaseUrl() => BaseUrl;
        public void SaveBaseUrl(string baseUrl) => BaseUrl = baseUrl;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
        public IReadOnlyList<string> GetModelsForProvider(AiProviderType provider) => AiProviderDefaults.GetDefaultModels(provider);
    }

    private class TestWorkoutSessionRepository : IWorkoutSessionRepository
    {
        public WorkoutSessionDetail? SessionDetail { get; set; }
        public ExerciseHistorySummary? ExerciseHistory { get; set; }

        public Task<WorkoutSessionDetail?> GetSessionDetailAsync(int sessionId) => Task.FromResult(SessionDetail);
        public Task<ExerciseHistorySummary> GetExerciseHistoryAsync(int exerciseId) => Task.FromResult(ExerciseHistory ?? new ExerciseHistorySummary());

        public Task<ActiveSessionSummary?> GetActiveSessionSummaryAsync() => Task.FromResult<ActiveSessionSummary?>(null);
        public Task<int> StartSessionFromProgramWorkoutAsync(int programWorkoutId) => Task.FromResult(1);
        public Task<int> StartEmptySessionAsync(string title) => Task.FromResult(1);
        public Task<SessionExerciseDetail> AddExerciseToSessionAsync(int sessionId, int exerciseId) => throw new NotImplementedException();
        public Task DeleteExerciseFromSessionAsync(int workoutExerciseId) => Task.CompletedTask;
        public Task<SetEntry> AddSetAsync(int workoutExerciseId, double weight, int reps, double? rpe, string? comment = null) => throw new NotImplementedException();
        public Task DeleteSetAsync(int setId) => Task.CompletedTask;
        public Task FinishSessionAsync(int sessionId, string? comment = null) => Task.CompletedTask;
        public Task DiscardSessionAsync(int sessionId) => Task.CompletedTask;
        public Task<IReadOnlyList<PastWorkoutSummary>> GetPastSessionsAsync() => Task.FromResult<IReadOnlyList<PastWorkoutSummary>>([]);
        public Task<int> GetCompletedSessionsCountAsync() => Task.FromResult(0);
        public Task<IReadOnlyList<DateTime>> GetCompletedSessionDatesAsync(DateTime fromUtc, DateTime toUtc) => Task.FromResult<IReadOnlyList<DateTime>>([]);
        public Task<IReadOnlyList<WorkoutSessionDetail>> GetSessionsForDateAsync(DateTime date) => Task.FromResult<IReadOnlyList<WorkoutSessionDetail>>([]);
    }

    [Fact]
    public async Task TestConnectionAsync_Success_ReturnsSuccessResult()
    {
        var mockResponse = """
        {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            { "text": "Подключено" }
                        ]
                    },
                    "finishReason": "STOP"
                }
            ]
        }
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            Assert.Contains("key=test-fake-key", req.RequestUri!.Query);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });
        });

        var settings = new TestAiSettingsService();
        var repo = new TestWorkoutSessionRepository();
        using var client = new HttpClient(handler);
        var service = new GeminiAiAnalysisService(settings, repo, client);

        var result = await service.TestConnectionAsync("test-fake-key", "gemini-2.5-flash");

        Assert.True(result.IsSuccess);
        Assert.Contains("успешно проверена", result.Message);
    }

    [Fact]
    public async Task TestConnectionAsync_EmptyApiKey_ReturnsFailureWithoutHttpCall()
    {
        bool wasCalled = false;
        var handler = new MockHttpMessageHandler(req =>
        {
            wasCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var settings = new TestAiSettingsService();
        var repo = new TestWorkoutSessionRepository();
        using var client = new HttpClient(handler);
        var service = new GeminiAiAnalysisService(settings, repo, client);

        var result = await service.TestConnectionAsync("", "gemini-2.5-flash");

        Assert.False(result.IsSuccess);
        Assert.False(wasCalled);
        Assert.Contains("API-ключ не указан", result.Message);
    }

    [Fact]
    public async Task AnalyzeWorkoutSessionAsync_ValidSession_ReturnsAiAnalysis()
    {
        var mockResponse = """
        {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            { "text": "## Разбор тренировки\nОтличная работа с жимом!" }
                        ]
                    },
                    "finishReason": "STOP"
                }
            ]
        }
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });
        });

        var settings = new TestAiSettingsService();
        var repo = new TestWorkoutSessionRepository
        {
            SessionDetail = new WorkoutSessionDetail
            {
                Id = 1,
                Title = "Жим лежа",
                StartedAt = DateTime.UtcNow,
                Exercises =
                [
                    new SessionExerciseDetail
                    {
                        ExerciseId = 1,
                        ExerciseName = "Жим",
                        Sets = [new SetEntry { SetNumber = 1, Weight = 100, Reps = 10 }]
                    }
                ]
            }
        };

        using var client = new HttpClient(handler);
        var service = new GeminiAiAnalysisService(settings, repo, client);

        var result = await service.AnalyzeWorkoutSessionAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Contains("Разбор тренировки", result.Content);
        Assert.Contains("Отличная работа с жимом!", result.Content);
        Assert.Equal("gemini-2.5-flash", result.ModelUsed);
    }

    [Fact]
    public async Task AnalyzeWorkoutSessionAsync_NoApiKey_ReturnsFailure()
    {
        var settings = new TestAiSettingsService { ApiKey = null };
        var repo = new TestWorkoutSessionRepository();
        var service = new GeminiAiAnalysisService(settings, repo);

        var result = await service.AnalyzeWorkoutSessionAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Contains("API-ключ не задан", result.ErrorMessage);
    }

    [Fact]
    public async Task AnalyzeWorkoutSessionAsync_InvalidApiKey400_ReturnsHelpfulError()
    {
        var errorResponse = """
        {
            "error": {
                "code": 400,
                "message": "API_KEY_INVALID: Please pass a valid API key.",
                "status": "INVALID_ARGUMENT"
            }
        }
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorResponse, Encoding.UTF8, "application/json")
            });
        });

        var settings = new TestAiSettingsService { ApiKey = "bad_key" };
        var repo = new TestWorkoutSessionRepository
        {
            SessionDetail = new WorkoutSessionDetail { Id = 1, Title = "Тест", StartedAt = DateTime.UtcNow }
        };

        using var client = new HttpClient(handler);
        var service = new GeminiAiAnalysisService(settings, repo, client);

        var result = await service.AnalyzeWorkoutSessionAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Contains("Недействительный API-ключ", result.ErrorMessage);
    }

    [Fact]
    public async Task AnalyzeWorkoutSessionAsync_RateLimit429_ReturnsRateLimitError()
    {
        var errorResponse = """
        {
            "error": {
                "code": 429,
                "message": "Resource has been exhausted (e.g. check quota).",
                "status": "RESOURCE_EXHAUSTED"
            }
        }
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(errorResponse, Encoding.UTF8, "application/json")
            });
        });

        var settings = new TestAiSettingsService { ApiKey = "valid_key" };
        var repo = new TestWorkoutSessionRepository
        {
            SessionDetail = new WorkoutSessionDetail { Id = 1, Title = "Тест", StartedAt = DateTime.UtcNow }
        };

        using var client = new HttpClient(handler);
        var service = new GeminiAiAnalysisService(settings, repo, client);

        var result = await service.AnalyzeWorkoutSessionAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Contains("429 Too Many Requests", result.ErrorMessage);
    }

    [Fact]
    public async Task AnalyzeExerciseProgressAsync_ValidExercise_ReturnsInsights()
    {
        var mockResponse = """
        {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            { "text": "## Анализ прогресса\nРост 1ПМ составил 10 кг за 2 месяца." }
                        ]
                    },
                    "finishReason": "STOP"
                }
            ]
        }
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });
        });

        var settings = new TestAiSettingsService();
        var repo = new TestWorkoutSessionRepository
        {
            ExerciseHistory = new ExerciseHistorySummary
            {
                ExerciseId = 5,
                ExerciseName = "Приседания",
                TotalSessionsCount = 3
            }
        };

        using var client = new HttpClient(handler);
        var service = new GeminiAiAnalysisService(settings, repo, client);

        var result = await service.AnalyzeExerciseProgressAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Contains("Анализ прогресса", result.Content);
        Assert.Contains("Рост 1ПМ", result.Content);
    }

    [Fact]
    public async Task TestConnectionAsync_OpenAiCompatible_Success()
    {
        var mockResponse = """
        {
            "choices": [
                {
                    "message": {
                        "role": "assistant",
                        "content": "Готов"
                    }
                }
            ]
        }
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
            Assert.Equal("deepseek-key-123", req.Headers.Authorization?.Parameter);
            Assert.Contains("/chat/completions", req.RequestUri!.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });
        });

        var settings = new TestAiSettingsService
        {
            Provider = AiProviderType.DeepSeek,
            ApiKey = "deepseek-key-123",
            SelectedModel = "deepseek-chat",
            BaseUrl = "https://api.deepseek.com/v1"
        };
        var repo = new TestWorkoutSessionRepository();
        using var client = new HttpClient(handler);
        var service = new GeminiAiAnalysisService(settings, repo, client);

        var result = await service.TestConnectionAsync(AiProviderType.DeepSeek, "deepseek-key-123", "deepseek-chat", "https://api.deepseek.com/v1");

        Assert.True(result.IsSuccess);
        Assert.Contains("успешно проверена", result.Message);
        Assert.Contains("DeepSeek", result.Message);
    }

    [Fact]
    public async Task AnalyzeWorkoutSessionAsync_OpenAiProvider_ReturnsAnalysis()
    {
        var mockResponse = """
        {
            "choices": [
                {
                    "message": {
                        "role": "assistant",
                        "content": "## Сводка тренировки\nОтличный прогресс в жиме!"
                    }
                }
            ]
        }
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal("Bearer sk-test-key", req.Headers.Authorization?.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });
        });

        var settings = new TestAiSettingsService
        {
            Provider = AiProviderType.OpenAI,
            ApiKey = "sk-test-key",
            SelectedModel = "gpt-4o-mini",
            BaseUrl = "https://api.openai.com/v1"
        };
        var repo = new TestWorkoutSessionRepository
        {
            SessionDetail = new WorkoutSessionDetail
            {
                Id = 1,
                Title = "Жим",
                StartedAt = DateTime.UtcNow,
                Exercises = []
            }
        };

        using var client = new HttpClient(handler);
        var service = new GeminiAiAnalysisService(settings, repo, client);

        var result = await service.AnalyzeWorkoutSessionAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Contains("Сводка тренировки", result.Content);
        Assert.Equal("gpt-4o-mini", result.ModelUsed);
    }
}
