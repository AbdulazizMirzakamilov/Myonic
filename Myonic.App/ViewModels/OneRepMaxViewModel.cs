using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Calculators;

namespace Myonic.ViewModels;

public sealed class FormulaResultItem
{
    public string FormulaName { get; init; } = string.Empty;
    public string ValueText { get; init; } = string.Empty;
}

public partial class OneRepMaxViewModel : ObservableObject
{
    public const double MinWeight = 20.0;
    public const double MaxWeight = 500.0;

    public Myonic.Services.IActiveWorkoutTracker? ActiveWorkoutTracker { get; }

    public OneRepMaxViewModel(Myonic.Services.IActiveWorkoutTracker? activeWorkoutTracker = null)
    {
        ActiveWorkoutTracker = activeWorkoutTracker;
        _ = ActiveWorkoutTracker?.RefreshAsync();
    }

    // --- 1RM Calculation ---
    [ObservableProperty]
    private string _weightText = string.Empty;

    [ObservableProperty]
    private int _selectedRepsIndex = 4; // 5 reps by default

    [ObservableProperty]
    private string _averageText = "—";

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private bool _isLowAccuracy;

    [ObservableProperty]
    private string? _errorMessage;

    public List<int> RepOptions { get; } = Enumerable.Range(1, 12).ToList();
    public ObservableCollection<FormulaResultItem> FormulaResults { get; } = [];

    // --- Percentage Calculation ---
    [ObservableProperty]
    private string _percentWeightText = string.Empty;

    [ObservableProperty]
    private string _percentageText = string.Empty;

    [ObservableProperty]
    private string? _percentResultText;

    [ObservableProperty]
    private string? _percentDetailText;

    [ObservableProperty]
    private bool _hasPercentResult;

    [RelayCommand]
    public async Task CalculateAsync()
    {
        ErrorMessage = null;
        var cleanWeight = (WeightText ?? string.Empty).Replace(',', '.').Trim();

        if (!double.TryParse(cleanWeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight))
        {
            HasResult = false;
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите числовое значение веса.", "OK");
            return;
        }

        if (weight < MinWeight || weight > MaxWeight)
        {
            HasResult = false;
            await Shell.Current.DisplayAlertAsync("Ограничение по весу", $"Вес должен быть в диапазоне от {MinWeight:0} до {MaxWeight:0} кг.", "OK");
            return;
        }

        var reps = SelectedRepsIndex >= 0 && SelectedRepsIndex < RepOptions.Count
            ? RepOptions[SelectedRepsIndex]
            : 5;

        try
        {
            var result = OneRepMaxCalculator.Calculate(weight, reps);

            FormulaResults.Clear();
            foreach (var kvp in result.ByFormula)
            {
                FormulaResults.Add(new FormulaResultItem
                {
                    FormulaName = GetFormulaDisplayName(kvp.Key),
                    ValueText = $"{kvp.Value:0.#} кг"
                });
            }

            AverageText = $"{result.Average:0.#} кг";
            IsLowAccuracy = result.IsLowAccuracy;
            HasResult = true;
        }
        catch (Exception ex)
        {
            HasResult = false;
            await Shell.Current.DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    [RelayCommand]
    public async Task CalculatePercentageAsync()
    {
        var cleanWeight = (PercentWeightText ?? string.Empty).Replace(',', '.').Trim();
        if (!double.TryParse(cleanWeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight))
        {
            HasPercentResult = false;
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите базовый вес для расчёта процентов.", "OK");
            return;
        }

        if (weight < MinWeight || weight > MaxWeight)
        {
            HasPercentResult = false;
            await Shell.Current.DisplayAlertAsync("Ограничение по весу", $"Базовый вес должен быть от {MinWeight:0} до {MaxWeight:0} кг.", "OK");
            return;
        }

        var cleanPercent = (PercentageText ?? string.Empty).Replace(',', '.').Replace('%', ' ').Trim();
        if (!double.TryParse(cleanPercent, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) || percent <= 0 || percent > 150)
        {
            HasPercentResult = false;
            await Shell.Current.DisplayAlertAsync("Ошибка", "Процент должен быть числом от 1 до 150%.", "OK");
            return;
        }

        var calculatedWeight = weight * (percent / 100.0);

        PercentResultText = $"{calculatedWeight:0.##} кг";
        PercentDetailText = $"{percent:0.#}% от {weight:0.##} кг";
        HasPercentResult = true;
    }

    private static string GetFormulaDisplayName(OneRepMaxFormula formula) => formula switch
    {
        OneRepMaxFormula.Epley => "Эпли (Epley)",
        OneRepMaxFormula.Brzycki => "Бржицки (Brzycki)",
        OneRepMaxFormula.Lander => "Ландер (Lander)",
        OneRepMaxFormula.Lombardi => "Ломбарди (Lombardi)",
        OneRepMaxFormula.Mayhew => "Мэйхью (Mayhew)",
        OneRepMaxFormula.OConner => "О'Коннор (O'Conner)",
        OneRepMaxFormula.Wathan => "Уотан (Wathan)",
        _ => formula.ToString()
    };
}
