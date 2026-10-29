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

public sealed class PercentageItem
{
    public string PercentageText { get; init; } = string.Empty;
    public string WeightText { get; init; } = string.Empty;
}

public partial class OneRepMaxViewModel : ObservableObject
{
    [ObservableProperty]
    private string _weightText = "100";

    [ObservableProperty]
    private int _selectedRepsIndex = 4; // defaults to 5 reps

    [ObservableProperty]
    private string _averageText = "—";

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private bool _isLowAccuracy;

    public List<int> RepOptions { get; } = Enumerable.Range(1, 12).ToList();

    public ObservableCollection<FormulaResultItem> FormulaResults { get; } = [];
    public ObservableCollection<PercentageItem> Percentages { get; } = [];

    public OneRepMaxViewModel()
    {
        Calculate();
    }

    partial void OnWeightTextChanged(string value) => Calculate();
    partial void OnSelectedRepsIndexChanged(int value) => Calculate();

    [RelayCommand]
    public void Calculate()
    {
        var cleanWeight = (WeightText ?? string.Empty).Replace(',', '.').Trim();
        if (!double.TryParse(cleanWeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight) || weight <= 0)
        {
            HasResult = false;
            FormulaResults.Clear();
            Percentages.Clear();
            AverageText = "—";
            IsLowAccuracy = false;
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

            Percentages.Clear();
            int[] percents = [95, 90, 85, 80, 75, 70, 65, 60, 50];
            foreach (var p in percents)
            {
                Percentages.Add(new PercentageItem
                {
                    PercentageText = $"{p}%",
                    WeightText = $"{result.Average * (p / 100.0):0.#} кг"
                });
            }

            HasResult = true;
        }
        catch
        {
            HasResult = false;
        }
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
