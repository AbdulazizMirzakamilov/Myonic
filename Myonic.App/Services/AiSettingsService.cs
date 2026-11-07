using Microsoft.Maui.Storage;
using Myonic.Core.Models;
using Myonic.Core.Services;

namespace Myonic.Services;

public class AiSettingsService : IAiSettingsService
{
    private const string ProviderPref = "ai_provider";
    private const string ApiKeyPref = "ai_api_key";
    private const string LegacyApiKeyPref = "gemini_api_key";
    private const string ModelPref = "ai_model_name";
    private const string LegacyModelPref = "gemini_model_name";
    private const string BaseUrlPref = "ai_base_url";

    public AiProviderType Provider
    {
        get
        {
            var val = Preferences.Get(ProviderPref, (int)AiProviderType.Gemini);
            return Enum.IsDefined(typeof(AiProviderType), val) ? (AiProviderType)val : AiProviderType.Gemini;
        }
        set
        {
            Preferences.Set(ProviderPref, (int)value);
        }
    }

    public string? GetApiKey()
    {
        var key = Preferences.Get(ApiKeyPref, string.Empty);
        if (string.IsNullOrWhiteSpace(key))
        {
            key = Preferences.Get(LegacyApiKeyPref, string.Empty);
        }
        return key;
    }

    public void SaveApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Preferences.Remove(ApiKeyPref);
            Preferences.Remove(LegacyApiKeyPref);
        }
        else
        {
            Preferences.Set(ApiKeyPref, apiKey.Trim());
            Preferences.Set(LegacyApiKeyPref, apiKey.Trim());
        }
    }

    public string GetSelectedModel()
    {
        var model = Preferences.Get(ModelPref, string.Empty);
        if (string.IsNullOrWhiteSpace(model))
        {
            model = Preferences.Get(LegacyModelPref, string.Empty);
        }
        if (string.IsNullOrWhiteSpace(model))
        {
            var defaults = AiProviderDefaults.GetDefaultModels(Provider);
            return defaults.Count > 0 ? defaults[0] : "gemini-2.5-flash";
        }
        return model;
    }

    public void SaveSelectedModel(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            var defaults = AiProviderDefaults.GetDefaultModels(Provider);
            var def = defaults.Count > 0 ? defaults[0] : "gemini-2.5-flash";
            Preferences.Set(ModelPref, def);
        }
        else
        {
            Preferences.Set(ModelPref, modelName.Trim());
        }
    }

    public string GetBaseUrl()
    {
        var url = Preferences.Get(BaseUrlPref, string.Empty);
        if (string.IsNullOrWhiteSpace(url))
        {
            return AiProviderDefaults.GetDefaultBaseUrl(Provider);
        }
        return url;
    }

    public void SaveBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Preferences.Remove(BaseUrlPref);
        }
        else
        {
            Preferences.Set(BaseUrlPref, baseUrl.Trim().TrimEnd('/'));
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GetApiKey());

    public IReadOnlyList<string> GetModelsForProvider(AiProviderType provider)
    {
        return AiProviderDefaults.GetDefaultModels(provider);
    }
}
