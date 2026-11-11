using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Services;
using Myonic.Data;
using Myonic.Services;

namespace Myonic.ViewModels;

public partial class SettingsViewModel : ObservableObject, IHasActiveWorkoutTracker
{
    private readonly IThemeService _themeService;
    private readonly MyonicDatabase _database;
    private readonly IDataBackupService _backupService;
    private readonly IAiSettingsService _aiSettingsService;
    private readonly IAiAnalysisService _aiAnalysisService;
    private readonly INotificationSettingsService _notificationSettingsService;
    private readonly INotificationService _notificationService;
    private readonly string _dbPath;
    public IActiveWorkoutTracker? ActiveWorkoutTracker { get; }

    public SettingsViewModel(
        IThemeService themeService,
        MyonicDatabase database,
        IDataBackupService backupService,
        IAiSettingsService aiSettingsService,
        IAiAnalysisService aiAnalysisService,
        INotificationSettingsService notificationSettingsService,
        INotificationService notificationService,
        IActiveWorkoutTracker? activeWorkoutTracker = null)
    {
        _themeService = themeService;
        _database = database;
        _backupService = backupService;
        _aiSettingsService = aiSettingsService;
        _aiAnalysisService = aiAnalysisService;
        _notificationSettingsService = notificationSettingsService;
        _notificationService = notificationService;
        ActiveWorkoutTracker = activeWorkoutTracker;
        _dbPath = _database.DatabasePath;

        // Load current theme index (0: Dark, 1: Light, 2: System)
        _selectedThemeIndex = (int)_themeService.CurrentThemeMode;

        // Load workout preferences
        _showPrBadges = Preferences.Get("show_pr_badges", true);
        _soundVibrationEnabled = Preferences.Get("sound_vibration_enabled", true);

        // Load notification settings
        _enableUpcomingWorkoutReminders = _notificationSettingsService.EnableUpcomingWorkoutReminders;
        _upcomingWorkoutTime = _notificationSettingsService.UpcomingWorkoutTime;
        _enableMissedWorkoutAlerts = _notificationSettingsService.EnableMissedWorkoutAlerts;
        _missedWorkoutTime = _notificationSettingsService.MissedWorkoutTime;
        _enableWeightReminders = _notificationSettingsService.EnableWeightReminders;
        _weightReminderTime = _notificationSettingsService.WeightReminderTime;

        // Load AI settings
        _selectedProviderIndex = (int)_aiSettingsService.Provider;
        _geminiApiKey = _aiSettingsService.GetApiKey() ?? string.Empty;
        _selectedModel = _aiSettingsService.GetSelectedModel();
        _baseUrl = _aiSettingsService.GetBaseUrl();
        _isCustomBaseUrl = _aiSettingsService.Provider == AiProviderType.Custom;

        foreach (var p in Enum.GetValues<AiProviderType>())
        {
            Providers.Add(AiProviderDefaults.GetDisplayName(p));
        }

        UpdateModelsForProvider(_aiSettingsService.Provider);

        LoadDatabaseInfo();
        _ = LoadBackupsListAsync();
    }

    public ObservableCollection<BackupFileInfo> Backups { get; } = [];
    public ObservableCollection<string> Providers { get; } = [];
    public ObservableCollection<string> AvailableModels { get; } = [];

    [ObservableProperty]
    private int _selectedProviderIndex;

    [ObservableProperty]
    private string _geminiApiKey = string.Empty;

    [ObservableProperty]
    private string _selectedModel = "gemini-2.5-flash";

    [ObservableProperty]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private bool _isCustomBaseUrl;

    [ObservableProperty]
    private bool _isApiKeyVisible;

    public bool IsApiKeyObfuscated => !IsApiKeyVisible;
    public string ApiKeyVisibilityIcon => IsApiKeyVisible ? "🔒" : "👁️";

    [ObservableProperty]
    private bool _isTestingConnection;

    [ObservableProperty]
    private string? _connectionTestMessage;

    [ObservableProperty]
    private bool _isConnectionTestSuccess;

    [ObservableProperty]
    private bool _hasConnectionTestResult;

    partial void OnSelectedProviderIndexChanged(int value)
    {
        if (value >= 0 && value < Enum.GetValues<AiProviderType>().Length)
        {
            var provider = (AiProviderType)value;
            _aiSettingsService.Provider = provider;
            IsCustomBaseUrl = provider == AiProviderType.Custom;
            BaseUrl = AiProviderDefaults.GetDefaultBaseUrl(provider);

            UpdateModelsForProvider(provider);
            var defaultModels = AiProviderDefaults.GetDefaultModels(provider);
            if (defaultModels.Count > 0 && !defaultModels.Contains(SelectedModel))
            {
                SelectedModel = defaultModels[0];
            }
        }
    }

    private void UpdateModelsForProvider(AiProviderType provider)
    {
        AvailableModels.Clear();
        foreach (var m in AiProviderDefaults.GetDefaultModels(provider))
        {
            AvailableModels.Add(m);
        }
    }

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private bool _showPrBadges;

    [ObservableProperty]
    private bool _soundVibrationEnabled;

    // Notifications
    [ObservableProperty]
    private bool _enableUpcomingWorkoutReminders;

    [ObservableProperty]
    private TimeSpan _upcomingWorkoutTime;

    [ObservableProperty]
    private bool _enableMissedWorkoutAlerts;

    [ObservableProperty]
    private TimeSpan _missedWorkoutTime;

    [ObservableProperty]
    private bool _enableWeightReminders;

    [ObservableProperty]
    private TimeSpan _weightReminderTime;

    [ObservableProperty]
    private bool _isTestingNotification;

    public bool IsNotTestingNotification => !IsTestingNotification;

    [ObservableProperty]
    private string? _testNotificationMessage;

    [ObservableProperty]
    private bool _hasTestNotificationResult;

    [ObservableProperty]
    private bool _isTestNotificationSuccess;

    partial void OnIsTestingNotificationChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotTestingNotification));
    }

    partial void OnEnableUpcomingWorkoutRemindersChanged(bool value)
    {
        _notificationSettingsService.EnableUpcomingWorkoutReminders = value;
        _ = _notificationService.SyncScheduledRemindersAsync();
    }

    partial void OnUpcomingWorkoutTimeChanged(TimeSpan value)
    {
        _notificationSettingsService.UpcomingWorkoutTime = value;
        _ = _notificationService.SyncScheduledRemindersAsync();
    }

    partial void OnEnableMissedWorkoutAlertsChanged(bool value)
    {
        _notificationSettingsService.EnableMissedWorkoutAlerts = value;
        _ = _notificationService.SyncScheduledRemindersAsync();
    }

    partial void OnMissedWorkoutTimeChanged(TimeSpan value)
    {
        _notificationSettingsService.MissedWorkoutTime = value;
        _ = _notificationService.SyncScheduledRemindersAsync();
    }

    partial void OnEnableWeightRemindersChanged(bool value)
    {
        _notificationSettingsService.EnableWeightReminders = value;
        _ = _notificationService.SyncScheduledRemindersAsync();
    }

    partial void OnWeightReminderTimeChanged(TimeSpan value)
    {
        _notificationSettingsService.WeightReminderTime = value;
        _ = _notificationService.SyncScheduledRemindersAsync();
    }

    [RelayCommand]
    public async Task SendTestNotificationAsync()
    {
        if (IsTestingNotification) return;

        IsTestingNotification = true;
        HasTestNotificationResult = false;
        TestNotificationMessage = "Отправка тестового уведомления...";

        try
        {
            var granted = await _notificationService.RequestPermissionsAsync();
            if (!granted)
            {
                IsTestNotificationSuccess = false;
                TestNotificationMessage = "Разрешение на отправку уведомлений отклонено в системе";
                HasTestNotificationResult = true;
                return;
            }

            await _notificationService.ShowNotificationAsync(
                NotificationIds.TestNotificationId,
                "🔔 Тестовое уведомление Myonic",
                "Система локальных уведомлений работает штатно! Все напоминания настроены.",
                NotificationCategory.General);

            IsTestNotificationSuccess = true;
            TestNotificationMessage = "Тестовое уведомление успешно отправлено!";
            HasTestNotificationResult = true;

            await _notificationService.SyncScheduledRemindersAsync();
        }
        catch (Exception ex)
        {
            IsTestNotificationSuccess = false;
            TestNotificationMessage = $"Ошибка отправки: {ex.Message}";
            HasTestNotificationResult = true;
        }
        finally
        {
            IsTestingNotification = false;
        }
    }

    [ObservableProperty]
    private string _databaseSizeText = "Определение...";

    [ObservableProperty]
    private string _databasePathText = string.Empty;

    [ObservableProperty]
    private string _appVersionText = "1.0 (MVP)";

    [ObservableProperty]
    private bool _hasBackups;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (value >= 0 && value <= 2)
        {
            _themeService.SetThemeMode((AppThemeMode)value);
        }
    }

    partial void OnShowPrBadgesChanged(bool value)
    {
        Preferences.Set("show_pr_badges", value);
    }

    partial void OnSoundVibrationEnabledChanged(bool value)
    {
        Preferences.Set("sound_vibration_enabled", value);
    }

    partial void OnIsApiKeyVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsApiKeyObfuscated));
        OnPropertyChanged(nameof(ApiKeyVisibilityIcon));
    }

    [RelayCommand]
    public void ToggleApiKeyVisibility()
    {
        IsApiKeyVisible = !IsApiKeyVisible;
    }

    [RelayCommand]
    public async Task SaveAiSettingsAsync()
    {
        var provider = (AiProviderType)SelectedProviderIndex;
        _aiSettingsService.Provider = provider;
        await _aiSettingsService.SaveApiKeyAsync(GeminiApiKey);
        _aiSettingsService.SaveSelectedModel(SelectedModel);
        _aiSettingsService.SaveBaseUrl(BaseUrl);

        await Shell.Current.DisplayAlertAsync(
            "Настройки ИИ сохранены",
            string.IsNullOrWhiteSpace(GeminiApiKey)
                ? "API-ключ удалён. Модуль ИИ-аналитики отключён."
                : $"Провайдер {AiProviderDefaults.GetDisplayName(provider)}, модель ({SelectedModel}) и ключ сохранены.",
            "OK");
    }

    [RelayCommand]
    public async Task TestAiConnectionAsync()
    {
        if (IsTestingConnection) return;

        IsTestingConnection = true;
        HasConnectionTestResult = false;
        var provider = (AiProviderType)SelectedProviderIndex;
        ConnectionTestMessage = $"Проверка соединения с {AiProviderDefaults.GetDisplayName(provider)}...";

        try
        {
            _aiSettingsService.Provider = provider;
            await _aiSettingsService.SaveApiKeyAsync(GeminiApiKey);
            _aiSettingsService.SaveSelectedModel(SelectedModel);
            _aiSettingsService.SaveBaseUrl(BaseUrl);

            var result = await _aiAnalysisService.TestConnectionAsync(provider, GeminiApiKey, SelectedModel, BaseUrl);
            IsConnectionTestSuccess = result.IsSuccess;
            ConnectionTestMessage = result.Message;
            HasConnectionTestResult = true;
        }
        catch (Exception ex)
        {
            IsConnectionTestSuccess = false;
            ConnectionTestMessage = $"Ошибка проверки: {ex.Message}";
            HasConnectionTestResult = true;
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    public void SelectDarkTheme() => SelectedThemeIndex = (int)AppThemeMode.Dark;

    [RelayCommand]
    public void SelectLightTheme() => SelectedThemeIndex = (int)AppThemeMode.Light;

    [RelayCommand]
    public void SelectSystemTheme() => SelectedThemeIndex = (int)AppThemeMode.System;

    [RelayCommand]
    public async Task LoadBackupsListAsync()
    {
        try
        {
            var list = await _backupService.GetAvailableBackupsAsync();
            Backups.Clear();
            foreach (var item in list)
            {
                Backups.Add(item);
            }
            HasBackups = Backups.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] LoadBackupsListAsync error: {ex}");
        }
    }

    [RelayCommand]
    public async Task CreateSqliteBackupAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        BusyMessage = "Создание резервной копии SQLite...";
        try
        {
            var backup = await _backupService.CreateSqliteBackupAsync();
            await LoadBackupsListAsync();
            LoadDatabaseInfo();

            // Give user choice of destination using native Save As dialog
            try
            {
                await using var stream = File.OpenRead(backup.FilePath);
                var saveResult = await FileSaver.Default.SaveAsync(backup.FileName, stream);

                if (saveResult.IsSuccessful)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Резервная копия сохранена",
                        $"Файл успешно сохранён в выбранное место:\n{saveResult.FilePath}",
                        "Отлично");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] FileSaver error: {ex}");
            }

            // Fallback or if user dismissed dialog, notify of saved file
            await Shell.Current.DisplayAlertAsync(
                "Резервная копия сохранена",
                $"Файл сохранён в папку бэкапов:\n{backup.FilePath}\n\nРазмер: {backup.SizeFormatted}",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка бэкапа", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ExportJsonAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        BusyMessage = "Формирование экспорта JSON...";
        try
        {
            var exportFilePath = await _backupService.ExportToJsonAsync();
            await LoadBackupsListAsync();

            // Give user choice of destination using native Save As dialog
            try
            {
                await using var stream = File.OpenRead(exportFilePath);
                var fileName = Path.GetFileName(exportFilePath);
                var saveResult = await FileSaver.Default.SaveAsync(fileName, stream);

                if (saveResult.IsSuccessful)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Экспорт JSON сохранён",
                        $"Файл успешно сохранён в выбранное место:\n{saveResult.FilePath}",
                        "Отлично");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] FileSaver export error: {ex}");
            }

            // Fallback: share sheet
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Экспорт данных Myonic (JSON)",
                File = new ShareFile(exportFilePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка экспорта", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ImportJsonAsync()
    {
        if (IsBusy) return;
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите JSON-файл резервной копии Myonic"
            });

            if (result == null) return;

            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Подтверждение импорта",
                $"Вы выбрали файл «{result.FileName}».\n\nВнимание: импорт перезапишет текущую базу тренировок и программ данными из файла!\n\nПродолжить?",
                "Импортировать",
                "Отмена");

            if (!confirm) return;

            IsBusy = true;
            BusyMessage = "Импорт данных из JSON...";

            var jsonContent = await File.ReadAllTextAsync(result.FullPath);
            var summary = await _backupService.ImportFromJsonAsync(jsonContent);

            LoadDatabaseInfo();

            if (summary.Success)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Импорт завершён успешно! 🎉",
                    summary.ToDisplayString(),
                    "Отлично");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка импорта",
                    summary.ErrorMessage ?? "Не удалось импортировать данные.",
                    "OK");
            }

            await LoadBackupsListAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка импорта", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RestoreBackupAsync(BackupFileInfo? backup)
    {
        if (backup == null || IsBusy) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Восстановление из бэкапа",
            $"Восстановить базу данных из резервной копии «{backup.FileName}» ({backup.DateFormatted})?\n\nВсе текущие тренировки, созданные после этой даты, будут заменены.",
            "Восстановить",
            "Отмена");

        if (!confirm) return;

        IsBusy = true;
        BusyMessage = "Восстановление базы данных...";
        try
        {
            if (backup.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var json = await File.ReadAllTextAsync(backup.FilePath);
                var summary = await _backupService.ImportFromJsonAsync(json);
                if (!summary.Success)
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", summary.ErrorMessage ?? "Ошибка восстановления.", "OK");
                    return;
                }
            }
            else
            {
                await _backupService.RestoreSqliteBackupAsync(backup.FilePath);
            }

            LoadDatabaseInfo();

            await Shell.Current.DisplayAlertAsync(
                "База данных восстановлена 🎉",
                $"Данные успешно восстановлены из копии «{backup.FileName}».",
                "Отлично");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка восстановления", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ShareBackupAsync(BackupFileInfo? backup)
    {
        if (backup == null) return;
        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = backup.FileName,
                File = new ShareFile(backup.FilePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка отправки", ex.Message, "OK");
        }
    }

    [RelayCommand]
    public async Task DeleteBackupAsync(BackupFileInfo? backup)
    {
        if (backup == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Удаление резервной копии",
            $"Удалить файл «{backup.FileName}»?",
            "Удалить",
            "Отмена");

        if (!confirm) return;

        await _backupService.DeleteBackupFileAsync(backup.FilePath);
        await LoadBackupsListAsync();
    }

    [RelayCommand]
    public async Task ClearAllDataAsync()
    {
        if (IsBusy) return;

        // Step 1: First warning
        bool step1 = await Shell.Current.DisplayAlertAsync(
            "⚠️ Очистить все данные?",
            "Все тренировки, подходы, программы, замеры тела и цели будут безвозвратно удалены!\n\nБазовый справочник упражнений останется нетронутым.\n\nВы уверены?",
            "Да, продолжить",
            "Отмена");

        if (!step1) return;

        // Step 2: Final confirmation
        bool step2 = await Shell.Current.DisplayAlertAsync(
            "🚨 ПОДТВЕРЖДЕНИЕ УДАЛЕНИЯ",
            "Это действие НЕЛЬЗЯ отменить. Вся ваша история силовых тренировок будет стёрта.\n\nРекомендуем сначала сделать резервную копию.\n\nТочно удалить все данные?",
            "УДАЛИТЬ ВСЁ",
            "Отмена");

        if (!step2) return;

        IsBusy = true;
        BusyMessage = "Очистка пользовательских данных...";
        try
        {
            await _backupService.ClearAllUserDataAsync(keepDefaultExercises: true);
            LoadDatabaseInfo();

            await Shell.Current.DisplayAlertAsync(
                "Данные очищены",
                "Все пользовательские данные успешно удалены. Базовые упражнения сохранены.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка очистки", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadDatabaseInfo()
    {
        try
        {
            DatabasePathText = _dbPath;
            if (File.Exists(_dbPath))
            {
                var fi = new FileInfo(_dbPath);
                var sizeKb = fi.Length / 1024.0;
                DatabaseSizeText = sizeKb >= 1024
                    ? $"{sizeKb / 1024.0:F2} МБ"
                    : $"{sizeKb:F1} КБ";
            }
            else
            {
                DatabaseSizeText = "0 КБ (новая)";
            }
        }
        catch
        {
            DatabaseSizeText = "Недоступно";
        }
    }
}
