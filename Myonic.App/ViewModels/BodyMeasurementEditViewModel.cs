using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Core.Services;

namespace Myonic.ViewModels;

public partial class BodyMeasurementEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly IBodyMeasurementRepository _repo;
    private readonly INotificationService? _notificationService;

    public BodyMeasurementEditViewModel(IBodyMeasurementRepository repo, INotificationService? notificationService = null)
    {
        _repo = repo;
        _notificationService = notificationService;
    }

    private int _measurementId;
    public int MeasurementId
    {
        get => _measurementId;
        set => _measurementId = value;
    }

    public DateTime MaxDate => DateTime.Today;

    [ObservableProperty]
    private string _title = "Новый замер";

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private string _weightText = "";

    [ObservableProperty]
    private string _bodyFatText = "";

    [ObservableProperty]
    private string _neckText = "";

    [ObservableProperty]
    private string _chestText = "";

    [ObservableProperty]
    private string _waistText = "";

    [ObservableProperty]
    private string _hipsText = "";

    [ObservableProperty]
    private string _bicepText = "";

    [ObservableProperty]
    private string _thighText = "";

    [ObservableProperty]
    private string _calfText = "";

    [ObservableProperty]
    private string _notes = "";

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            if (query.TryGetValue("id", out var val) && int.TryParse(val?.ToString(), out var id) && id > 0)
            {
                _measurementId = id;
                await LoadExistingAsync(id);
            }
            else
            {
                await InitNewMeasurementAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BodyMeasurementEditViewModel] ApplyQueryAttributes error: {ex}");
        }
    }

    private async Task InitNewMeasurementAsync()
    {
        Title = "Новый замер";
        IsEditing = false;
        Date = DateTime.Today;

        var latest = await _repo.GetLatestAsync();
        if (latest != null && latest.WeightKg > 0)
        {
            WeightText = latest.WeightKg.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            WeightText = "70.0";
        }
    }

    private async Task LoadExistingAsync(int id)
    {
        var m = await _repo.GetByIdAsync(id);
        if (m == null) return;

        Title = "Редактирование замера";
        IsEditing = true;
        Date = m.Date;
        WeightText = m.WeightKg > 0 ? m.WeightKg.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) : "";
        BodyFatText = m.BodyFatPercentage?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        NeckText = m.NeckCm?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        ChestText = m.ChestCm?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        WaistText = m.WaistCm?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        HipsText = m.HipsCm?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        BicepText = m.BicepsCm?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        ThighText = m.ThighCm?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        CalfText = m.CalfCm?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        Notes = m.Notes ?? "";
    }

    [RelayCommand]
    private void AdjustWeight(string? delta)
    {
        if (string.IsNullOrWhiteSpace(delta)) return;

        if (double.TryParse(delta.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
        {
            var current = ParseDouble(WeightText);
            if (current <= 0) current = 70.0;
            var next = Math.Max(0.5, Math.Round(current + d, 1));
            WeightText = next.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var weight = ParseDouble(WeightText);
        if (weight <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите корректный вес.", "OK");
            return;
        }

        try
        {
            if (IsEditing && MeasurementId > 0)
            {
                var existing = await _repo.GetByIdAsync(MeasurementId);
                if (existing != null)
                {
                    existing.Date = Date;
                    existing.WeightKg = weight;
                    existing.BodyFatPercentage = ParseNullableDouble(BodyFatText);
                    existing.NeckCm = ParseNullableDouble(NeckText);
                    existing.ChestCm = ParseNullableDouble(ChestText);
                    existing.WaistCm = ParseNullableDouble(WaistText);
                    existing.HipsCm = ParseNullableDouble(HipsText);
                    existing.BicepsCm = ParseNullableDouble(BicepText);
                    existing.ThighCm = ParseNullableDouble(ThighText);
                    existing.CalfCm = ParseNullableDouble(CalfText);
                    existing.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();

                    await _repo.UpdateAsync(existing);
                }
            }
            else
            {
                var m = new BodyMeasurement
                {
                    Date = Date,
                    WeightKg = weight,
                    BodyFatPercentage = ParseNullableDouble(BodyFatText),
                    NeckCm = ParseNullableDouble(NeckText),
                    ChestCm = ParseNullableDouble(ChestText),
                    WaistCm = ParseNullableDouble(WaistText),
                    HipsCm = ParseNullableDouble(HipsText),
                    BicepsCm = ParseNullableDouble(BicepText),
                    ThighCm = ParseNullableDouble(ThighText),
                    CalfCm = ParseNullableDouble(CalfText),
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
                };
                await _repo.AddAsync(m);
            }

            if (_notificationService != null)
            {
                _ = _notificationService.SyncScheduledRemindersAsync();
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BodyMeasurementEditViewModel] SaveAsync error: {ex}");
            await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось сохранить замер: " + ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsEditing || MeasurementId <= 0) return;

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Удаление замера", "Удалить этот замер?", "Удалить", "Отмена");

        if (confirmed)
        {
            try
            {
                await _repo.DeleteAsync(MeasurementId);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BodyMeasurementEditViewModel] DeleteAsync error: {ex}");
                await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось удалить замер: " + ex.Message, "OK");
            }
        }
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");

    private static double ParseDouble(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static double? ParseNullableDouble(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
