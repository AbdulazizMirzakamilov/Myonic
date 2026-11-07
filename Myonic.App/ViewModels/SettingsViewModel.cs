using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Services;
using Myonic.Data;
using Myonic.Services;

namespace Myonic.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly MyonicDatabase _database;
    private readonly IDataBackupService _backupService;
    private readonly string _dbPath;

    public SettingsViewModel(
        IThemeService themeService,
        MyonicDatabase database,
        IDataBackupService backupService)
    {
        _themeService = themeService;
        _database = database;
        _backupService = backupService;
        _dbPath = _database.DatabasePath;

        // Load current theme index (0: Dark, 1: Light, 2: System)
        _selectedThemeIndex = (int)_themeService.CurrentThemeMode;

        // Load workout preferences
        _showPrBadges = Preferences.Get("show_pr_badges", true);
        _soundVibrationEnabled = Preferences.Get("sound_vibration_enabled", true);

        LoadDatabaseInfo();
        _ = LoadBackupsListAsync();
    }

    public ObservableCollection<BackupFileInfo> Backups { get; } = [];

    [ObservableProperty]
    private int _selectedThemeIndex;

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

            await Shell.Current.DisplayAlertAsync(
                "Резервная копия создана",
                $"Атомарная копия SQLite успешно сохранена.\nФайл: {backup.FileName}\nРазмер: {backup.SizeFormatted}",
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
