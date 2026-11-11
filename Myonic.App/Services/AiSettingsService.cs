using Microsoft.Maui.Storage;
using Myonic.Core.Models;
using Myonic.Core.Services;

namespace Myonic.Services;

public class AiSettingsService : IAiSettingsService
{
    private const string ProviderPref = "ai_provider";
    public const string SecureApiKey = "secure_ai_api_key";
    public const string LegacyApiKeyPref = "ai_api_key";
    public const string AncientApiKeyPref = "gemini_api_key";
    private const string ModelPref = "ai_model_name";
    private const string LegacyModelPref = "gemini_model_name";
    private const string BaseUrlPref = "ai_base_url";

    private readonly ISecureStorage _secureStorage;
    private readonly IPreferences _preferences;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedApiKey;
    private bool _isLoaded;

    public AiSettingsService(ISecureStorage? secureStorage = null, IPreferences? preferences = null)
    {
        _secureStorage = secureStorage ?? SecureStorage.Default;
        _preferences = preferences ?? Preferences.Default;
    }

    public AiProviderType Provider
    {
        get
        {
            var val = _preferences.Get(ProviderPref, (int)AiProviderType.Gemini);
            return Enum.IsDefined(typeof(AiProviderType), val) ? (AiProviderType)val : AiProviderType.Gemini;
        }
        set
        {
            _preferences.Set(ProviderPref, (int)value);
        }
    }

    public async Task<string?> GetApiKeyAsync()
    {
        if (_isLoaded)
        {
            return _cachedApiKey;
        }

        await _lock.WaitAsync();
        try
        {
            if (_isLoaded)
            {
                return _cachedApiKey;
            }

            string? key = null;

            // 1. Попытка прочесть из защищённого хранилища (SecureStorage)
            try
            {
                key = await _secureStorage.GetAsync(SecureApiKey);
            }
            catch (Exception ex)
            {
                // Не логируем секреты, только тип возникшего исключения
                System.Diagnostics.Debug.WriteLine($"[AiSettingsService] SecureStorage read exception: {ex.GetType().Name}");
            }

            // 2. Если в SecureStorage пусто, проверяем Preferences (безопасная миграция старых ключей)
            if (string.IsNullOrWhiteSpace(key))
            {
                var legacyKey = _preferences.Get(LegacyApiKeyPref, string.Empty);
                if (string.IsNullOrWhiteSpace(legacyKey))
                {
                    legacyKey = _preferences.Get(AncientApiKeyPref, string.Empty);
                }

                if (!string.IsNullOrWhiteSpace(legacyKey))
                {
                    key = legacyKey.Trim();

                    // Безопасный перенос в SecureStorage
                    bool migrated = false;
                    try
                    {
                        await _secureStorage.SetAsync(SecureApiKey, key);
                        migrated = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AiSettingsService] SecureStorage migration write exception: {ex.GetType().Name}");
                    }

                    // 3. После успешного переноса удаляем старые значения из Preferences
                    if (migrated)
                    {
                        _preferences.Remove(LegacyApiKeyPref);
                        _preferences.Remove(AncientApiKeyPref);
                    }
                }
            }

            _cachedApiKey = string.IsNullOrWhiteSpace(key) ? null : key;
            _isLoaded = true;
            return _cachedApiKey;
        }
        finally
        {
            _lock.Release();
        }
    }

    public string? GetApiKey()
    {
        if (_isLoaded)
        {
            return _cachedApiKey;
        }

        try
        {
            return Task.Run(GetApiKeyAsync).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AiSettingsService] Sync GetApiKey fallback exception: {ex.GetType().Name}");
            var fallback = _preferences.Get(LegacyApiKeyPref, string.Empty);
            if (string.IsNullOrWhiteSpace(fallback))
            {
                fallback = _preferences.Get(AncientApiKeyPref, string.Empty);
            }
            return string.IsNullOrWhiteSpace(fallback) ? _cachedApiKey : fallback;
        }
    }

    public async Task SaveApiKeyAsync(string? apiKey)
    {
        await _lock.WaitAsync();
        try
        {
            var cleanKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
            _cachedApiKey = cleanKey;
            _isLoaded = true;

            if (cleanKey is null)
            {
                try
                {
                    _secureStorage.Remove(SecureApiKey);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AiSettingsService] SecureStorage remove exception: {ex.GetType().Name}");
                }

                _preferences.Remove(LegacyApiKeyPref);
                _preferences.Remove(AncientApiKeyPref);
            }
            else
            {
                try
                {
                    await _secureStorage.SetAsync(SecureApiKey, cleanKey);
                    // Очищаем незашифрованные настройки
                    _preferences.Remove(LegacyApiKeyPref);
                    _preferences.Remove(AncientApiKeyPref);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AiSettingsService] SecureStorage write exception: {ex.GetType().Name}");
                    // При временной недоступности SecureStorage ключ остаётся в оперативной памяти сессии
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public void SaveApiKey(string? apiKey)
    {
        try
        {
            Task.Run(() => SaveApiKeyAsync(apiKey)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AiSettingsService] Sync SaveApiKey fallback exception: {ex.GetType().Name}");
            _cachedApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
            _isLoaded = true;
        }
    }

    public string GetSelectedModel()
    {
        var model = _preferences.Get(ModelPref, string.Empty);
        if (string.IsNullOrWhiteSpace(model))
        {
            model = _preferences.Get(LegacyModelPref, string.Empty);
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
            _preferences.Set(ModelPref, def);
        }
        else
        {
            _preferences.Set(ModelPref, modelName.Trim());
        }
    }

    public string GetBaseUrl()
    {
        var url = _preferences.Get(BaseUrlPref, string.Empty);
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
            _preferences.Remove(BaseUrlPref);
        }
        else
        {
            _preferences.Set(BaseUrlPref, baseUrl.Trim().TrimEnd('/'));
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GetApiKey());

    public IReadOnlyList<string> GetModelsForProvider(AiProviderType provider)
    {
        return AiProviderDefaults.GetDefaultModels(provider);
    }
}
