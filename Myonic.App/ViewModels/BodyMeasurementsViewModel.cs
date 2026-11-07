using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Drawables;
using Myonic.Pages;

namespace Myonic.ViewModels;

public record MeasurementListItem(
    int Id,
    string DateText,
    string WeightText,
    string? DeltaText,
    bool DeltaIsNegative,
    bool DeltaIsPositive,
    bool HasBodyFat,
    string? BodyFatText,
    bool HasNotes,
    string? Notes);

public partial class BodyMeasurementsViewModel : ObservableObject
{
    private readonly IBodyMeasurementRepository _repo;

    public BodyMeasurementsViewModel(IBodyMeasurementRepository repo)
    {
        _repo = repo;
        Drawable = new WeightTrendDrawable();
    }

    public ObservableCollection<MeasurementListItem> Measurements { get; } = [];

    public WeightTrendDrawable Drawable { get; }

    public event Action? RequestChartRedraw;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasMeasurements;

    [ObservableProperty]
    private bool _hasNoMeasurements;

    // Trend stats
    [ObservableProperty]
    private string _currentWeightText = "–";

    [ObservableProperty]
    private string _deltaText = "–";

    [ObservableProperty]
    private bool _deltaIsNegative;

    [ObservableProperty]
    private bool _deltaIsPositive;

    [ObservableProperty]
    private string _minMaxText = "–";

    [ObservableProperty]
    private string _measurementsCountText = "0 замеров";

    // Period filter
    [ObservableProperty]
    private int _periodIndex; // 0 = 30d, 1 = 90d, 2 = 180d, 3 = all

    private int PeriodDays => PeriodIndex switch
    {
        0 => 30,
        1 => 90,
        2 => 180,
        3 => 3650, // ~10 years = all
        _ => 30
    };

    public bool IsPeriod30 => PeriodIndex == 0;
    public bool IsPeriod90 => PeriodIndex == 1;
    public bool IsPeriod180 => PeriodIndex == 2;
    public bool IsPeriodAll => PeriodIndex == 3;

    [RelayCommand]
    public void SelectPeriod(string indexStr)
    {
        if (int.TryParse(indexStr, out var idx) && idx >= 0 && idx <= 3)
        {
            PeriodIndex = idx;
            OnPropertyChanged(nameof(IsPeriod30));
            OnPropertyChanged(nameof(IsPeriod90));
            OnPropertyChanged(nameof(IsPeriod180));
            OnPropertyChanged(nameof(IsPeriodAll));
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var days = PeriodDays;
            var stats = await _repo.GetWeightTrendStatsAsync(days);

            if (stats.MeasurementsCount > 0)
            {
                CurrentWeightText = $"{stats.CurrentWeight:F1} кг";
                var sign = stats.DeltaKg >= 0 ? "+" : "";
                DeltaText = $"{sign}{stats.DeltaKg:F1} кг";
                DeltaIsNegative = stats.DeltaKg < 0;
                DeltaIsPositive = stats.DeltaKg > 0;
                MinMaxText = $"{stats.MinWeight:F1} – {stats.MaxWeight:F1} кг";
                MeasurementsCountText = $"{stats.MeasurementsCount} {Pluralize(stats.MeasurementsCount, "замер", "замера", "замеров")}";
            }
            else
            {
                CurrentWeightText = "–";
                DeltaText = "–";
                DeltaIsNegative = false;
                DeltaIsPositive = false;
                MinMaxText = "–";
                MeasurementsCountText = "0 замеров";
            }

            // Load chart data
            var fromDate = DateTime.Today.AddDays(-days);
            var chartData = await _repo.GetWeightHistoryAsync(fromDate, 60);
            Drawable.DataPoints = chartData
                .Where(m => m.WeightKg > 0)
                .Select(m => new WeightDataPoint(m.Date, m.WeightKg))
                .ToList();

            // Load list
            var all = await _repo.GetAllAsync(limit: 100);
            Measurements.Clear();
            BodyMeasurement? prev = null;
            // Show newest first; compute delta from the entry immediately before it chronologically
            for (int i = 0; i < all.Count; i++)
            {
                var m = all[i];
                // all is descending by date, so previous chronologically is all[i+1]
                prev = i + 1 < all.Count ? all[i + 1] : null;

                string? deltaText = null;
                bool deltaNeg = false, deltaPos = false;
                if (prev != null && m.WeightKg > 0 && prev.WeightKg > 0)
                {
                    var d = Math.Round(m.WeightKg - prev.WeightKg, 1);
                    if (d != 0)
                    {
                        var s = d > 0 ? "+" : "";
                        deltaText = $"{s}{d:F1} кг";
                        deltaNeg = d < 0;
                        deltaPos = d > 0;
                    }
                }

                Measurements.Add(new MeasurementListItem(
                    m.Id,
                    m.Date.ToString("dd.MM.yyyy"),
                    $"{m.WeightKg:F1} кг",
                    deltaText,
                    deltaNeg,
                    deltaPos,
                    m.BodyFatPercentage.HasValue,
                    m.BodyFatPercentage.HasValue ? $"{m.BodyFatPercentage.Value:F1}% жира" : null,
                    !string.IsNullOrWhiteSpace(m.Notes),
                    m.Notes));
            }

            HasMeasurements = Measurements.Count > 0;
            HasNoMeasurements = !HasMeasurements;
        }
        finally
        {
            IsLoading = false;
            RequestChartRedraw?.Invoke();
        }
    }

    [RelayCommand]
    private Task AddAsync() =>
        Shell.Current.GoToAsync(nameof(BodyMeasurementEditPage));

    [RelayCommand]
    private Task EditAsync(MeasurementListItem item) =>
        Shell.Current.GoToAsync($"{nameof(BodyMeasurementEditPage)}?id={item.Id}");

    [RelayCommand]
    private async Task DeleteAsync(MeasurementListItem item)
    {
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Удаление замера",
            $"Удалить замер от {item.DateText}?",
            "Удалить", "Отмена");

        if (confirmed)
        {
            await _repo.DeleteAsync(item.Id);
            await LoadAsync();
        }
    }

    private static string Pluralize(int count, string one, string few, string many)
    {
        var abs = Math.Abs(count) % 100;
        if (abs >= 11 && abs <= 19) return many;
        var last = abs % 10;
        return last switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many
        };
    }
}
