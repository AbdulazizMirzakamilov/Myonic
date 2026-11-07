namespace Myonic.Core.Services;

public interface IAiSettingsService
{
    string? GetApiKey();
    void SaveApiKey(string? apiKey);
    string GetSelectedModel();
    void SaveSelectedModel(string modelName);
    bool IsConfigured { get; }
    IReadOnlyList<string> AvailableModels { get; }
}
