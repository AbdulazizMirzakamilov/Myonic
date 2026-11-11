using Myonic.Core.Models;

namespace Myonic.Core.Services;

public interface IAiSettingsService
{
    AiProviderType Provider { get; set; }
    string? GetApiKey();
    Task<string?> GetApiKeyAsync();
    void SaveApiKey(string? apiKey);
    Task SaveApiKeyAsync(string? apiKey);
    string GetSelectedModel();
    void SaveSelectedModel(string modelName);
    string GetBaseUrl();
    void SaveBaseUrl(string baseUrl);
    bool IsConfigured { get; }
    IReadOnlyList<string> GetModelsForProvider(AiProviderType provider);
}
