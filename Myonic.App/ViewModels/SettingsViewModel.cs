using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Services;

namespace Myonic.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly string _dbPath;

    public SettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "myonic.db3");

        // Load current theme index (0: Dark, 1: Light, 2: System)
        _selectedThemeIndex = (int)_themeService.CurrentThemeMode;

        // Load unit preferences (default: kg)
        _isKg = Preferences.Get("weight_unit_kg", true);

        // Load workout preferences
        _autoCalculate1Rm = Preferences.Get("auto_calc_1rm", true);
        _showPrBadges = Preferences.Get("show_pr_badges", true);
        _soundVibrationEnabled = Preferences.Get("sound_vibration_enabled", true);

        LoadDatabaseInfo();
    }

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private bool _isKg;

    [ObservableProperty]
    private bool _autoCalculate1Rm;

    [ObservableProperty]
    private bool _showPrBadges;

    [ObservableProperty]
    private bool _soundVibrationEnabled;

    [ObservableProperty]
    private string _databaseSizeText = "Определение...";

    [ObservableProperty]
    private string _databasePathText = string.Empty;

    [ObservableProperty]
    private string _appVersionText = "1.0 (MVP)";

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (value >= 0 && value <= 2)
        {
            _themeService.SetThemeMode((AppThemeMode)value);
        }
    }

    partial void OnIsKgChanged(bool value)
    {
        Preferences.Set("weight_unit_kg", value);
    }

    partial void OnAutoCalculate1RmChanged(bool value)
    {
        Preferences.Set("auto_calc_1rm", value);
    }

    partial void OnShowPrBadgesChanged(bool value)
    {
        Preferences.Set("show_pr_badges", value);
    }

    partial void OnSoundVibrationEnabledChanged(bool value)
    {
        Preferences.Set("sound_vibration_enabled", value);
    }

    [RelayCommand]
    public void SelectDarkTheme()
    {
        SelectedThemeIndex = (int)AppThemeMode.Dark;
    }

    [RelayCommand]
    public void SelectLightTheme()
    {
        SelectedThemeIndex = (int)AppThemeMode.Light;
    }

    [RelayCommand]
    public void SelectSystemTheme()
    {
        SelectedThemeIndex = (int)AppThemeMode.System;
    }

    [RelayCommand]
    public async Task BackupDatabaseAsync()
    {
        try
        {
            if (!File.Exists(_dbPath))
            {
                await Shell.Current.DisplayAlertAsync("Резервная копия", "Файл базы данных пока не создан.", "OK");
                return;
            }

            var backupDir = Path.Combine(FileSystem.CacheDirectory, "Backups");
            Directory.CreateDirectory(backupDir);
            var backupPath = Path.Combine(backupDir, $"myonic_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db3");

            File.Copy(_dbPath, backupPath, overwrite: true);

            var fileInfo = new FileInfo(backupPath);
            var sizeKb = Math.Max(1, fileInfo.Length / 1024);

            await Shell.Current.DisplayAlertAsync(
                "Резервная копия создана",
                $"Локальный бэкап успешно сохранён ({sizeKb} КБ).\nФайл: {Path.GetFileName(backupPath)}",
                "Отлично");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка бэкапа", ex.Message, "OK");
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
