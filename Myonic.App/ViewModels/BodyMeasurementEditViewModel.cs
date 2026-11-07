using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.ViewModels;

[QueryProperty(nameof(MeasurementId), "id")]
public partial class BodyMeasurementEditViewModel : ObservableObject
{
    private readonly IBodyMeasurementRepository _repo;

    public BodyMeasurementEditViewModel(IBodyMeasurementRepository repo) => _repo = repo;

    private int _measurementId;
    public int MeasurementId
    {
        get => _measurementId;
        set
        {
            _measurementId = value;
            if (value > 0) _ = LoadExistingAsync(value);
        }
    }

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

    private async Task LoadExistingAsync(int id)
    {
        var m = await _repo.GetByIdAsync(id);
        if (m == null) return;

        Title = "Редактирование замера";
        IsEditing = true;
        Date = m.Date;
        WeightText = m.WeightKg > 0 ? m.WeightKg.ToString("F1") : "";
        BodyFatText = m.BodyFatPercentage?.ToString("F1") ?? "";
        NeckText = m.NeckCm?.ToString("F1") ?? "";
        ChestText = m.ChestCm?.ToString("F1") ?? "";
        WaistText = m.WaistCm?.ToString("F1") ?? "";
        HipsText = m.HipsCm?.ToString("F1") ?? "";
        BicepText = m.BicepsCm?.ToString("F1") ?? "";
        ThighText = m.ThighCm?.ToString("F1") ?? "";
        CalfText = m.CalfCm?.ToString("F1") ?? "";
        Notes = m.Notes ?? "";
    }

    [RelayCommand]
    private void AdjustWeight(string delta)
    {
        if (double.TryParse(delta, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
        {
            var current = ParseDouble(WeightText);
            var next = Math.Max(0, current + d);
            WeightText = next.ToString("F1");
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

        if (IsEditing && MeasurementId > 0)
        {
            var existing = await _repo.GetByIdAsync(MeasurementId);
            if (existing == null) return;

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

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsEditing || MeasurementId <= 0) return;

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Удаление замера", "Удалить этот замер?", "Удалить", "Отмена");

        if (confirmed)
        {
            await _repo.DeleteAsync(MeasurementId);
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");

    private static double ParseDouble(string text) =>
        double.TryParse(text?.Replace(',', '.'),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static double? ParseNullableDouble(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return double.TryParse(text.Replace(',', '.'),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
