using Microsoft.Maui.Storage;
using Myonic.Core.Services;

namespace Myonic.Services;

public class AiSettingsService : IAiSettingsService
{
    private const string ApiKeyPref = "gemini_api_key";
    private const string ModelPref = "gemini_model_name";
    private const string DefaultModel = "gemini-2.5-flash";

    public static readonly string[] SupportedModels =
    [
        "gemini-2.5-flash",
        "gemini-1.5-flash",
        "gemini-2.0-flash"
    ];

    public IReadOnlyList<string> AvailableModels => SupportedModels;

    public string? GetApiKey()
    {
        return Preferences.Get(ApiKeyPref, string.Empty);
    }

    public void SaveApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Preferences.Remove(ApiKeyPref);
        }
        else
        {
            Preferences.Set(ApiKeyPref, apiKey.Trim());
        }
    }

    public string GetSelectedModel()
    {
        var model = Preferences.Get(ModelPref, DefaultModel);
        return string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
    }

    public void SaveSelectedModel(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            Preferences.Set(ModelPref, DefaultModel);
        }
        else
        {
            Preferences.Set(ModelPref, modelName.Trim());
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GetApiKey());
}
