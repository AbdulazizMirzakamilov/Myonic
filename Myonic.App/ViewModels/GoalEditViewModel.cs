using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.ViewModels;

public partial class GoalEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly IGoalRepository _goalRepo;
    private readonly IExerciseRepository _exerciseRepo;
    private readonly IPersonalRecordRepository _prRepo;
    private readonly IBodyMeasurementRepository _bodyMeasurementRepo;

    public GoalEditViewModel(
        IGoalRepository goalRepo,
        IExerciseRepository exerciseRepo,
        IPersonalRecordRepository prRepo,
        IBodyMeasurementRepository bodyMeasurementRepo)
    {
        _goalRepo = goalRepo;
        _exerciseRepo = exerciseRepo;
        _prRepo = prRepo;
        _bodyMeasurementRepo = bodyMeasurementRepo;
    }

    private int _goalId;
    public int GoalId => _goalId;

    public DateTime MinDate => DateTime.Today;

    [ObservableProperty]
    private string _title = "Новая цель";

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _goalTitle = "";

    [ObservableProperty]
    private int _goalTypeIndex = 0; // 0: ExerciseWeight, 1: BodyWeight, 2: BodyMeasurement, 3: WorkoutsFrequency

    public ObservableCollection<string> GoalTypeOptions { get; } =
    [
        "Сила в упражнении (кг)",
        "Вес тела (кг)",
        "Замер тела (см)",
        "Регулярность тренировок (в месяц)"
    ];

    public ObservableCollection<Exercise> Exercises { get; } = [];

    [ObservableProperty]
    private Exercise? _selectedExercise;

    public ObservableCollection<string> BodyPartOptions { get; } =
    [
        "Бицепс",
        "Грудь",
        "Талия",
        "Бёдра",
        "Шея",
        "Голень"
    ];

    [ObservableProperty]
    private string _selectedBodyPart = "Бицепс";

    [ObservableProperty]
    private string _startValueText = "";

    [ObservableProperty]
    private string _targetValueText = "";

    [ObservableProperty]
    private string _currentValueText = "";

    [ObservableProperty]
    private bool _hasTargetDate;

    [ObservableProperty]
    private DateTime _targetDate = DateTime.Today.AddMonths(1);

    [ObservableProperty]
    private int _statusIndex = 0; // 0: Active, 1: Achieved, 2: Archived

    public ObservableCollection<string> StatusOptions { get; } =
    [
        "Активная",
        "Достигнута 🎉",
        "В архиве"
    ];

    [ObservableProperty]
    private string _notes = "";

    public bool IsExerciseGoal => GoalTypeIndex == 0;
    public bool IsBodyWeightGoal => GoalTypeIndex == 1;
    public bool IsMeasurementGoal => GoalTypeIndex == 2;
    public bool IsFrequencyGoal => GoalTypeIndex == 3;

    public string ValueUnitText => GoalTypeIndex switch
    {
        0 => "кг",
        1 => "кг",
        2 => "см",
        3 => "трен.",
        _ => ""
    };

    partial void OnGoalTypeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsExerciseGoal));
        OnPropertyChanged(nameof(IsBodyWeightGoal));
        OnPropertyChanged(nameof(IsMeasurementGoal));
        OnPropertyChanged(nameof(IsFrequencyGoal));
        OnPropertyChanged(nameof(ValueUnitText));

        if (!IsEditing)
        {
            _ = HandleGoalTypeChangedAsync(value);
        }
    }

    partial void OnSelectedExerciseChanged(Exercise? value)
    {
        if (value != null && !IsEditing)
        {
            if (string.IsNullOrWhiteSpace(GoalTitle) || GoalTitle.StartsWith("Жим") || GoalTitle.StartsWith("Присед") || GoalTitle.StartsWith("Тяга") || GoalTitle.StartsWith("Сила:"))
            {
                GoalTitle = $"{value.Name}";
            }
            _ = FetchPrForExerciseAsync(value.Id);
        }
    }

    partial void OnSelectedBodyPartChanged(string value)
    {
        if (!IsEditing && IsMeasurementGoal)
        {
            GoalTitle = $"Обхват: {value}";
            _ = FetchMeasurementForPartAsync(value);
        }
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            await LoadExercisesListAsync();

            if (query.TryGetValue("id", out var val) && int.TryParse(val?.ToString(), out var id) && id > 0)
            {
                _goalId = id;
                await LoadExistingGoalAsync(id);
            }
            else
            {
                await InitNewGoalAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GoalEditViewModel] ApplyQueryAttributes error: {ex}");
        }
    }

    private async Task LoadExercisesListAsync()
    {
        var list = await _exerciseRepo.GetAllAsync();
        Exercises.Clear();
        foreach (var ex in list.OrderBy(e => e.Name))
        {
            Exercises.Add(ex);
        }
    }

    private async Task InitNewGoalAsync()
    {
        Title = "Новая цель";
        IsEditing = false;
        GoalTypeIndex = 0;
        StatusIndex = 0;
        HasTargetDate = false;
        TargetDate = DateTime.Today.AddMonths(1);
        Notes = "";

        if (Exercises.Count > 0)
        {
            SelectedExercise = Exercises.FirstOrDefault(e => e.Name.Contains("жим", StringComparison.OrdinalIgnoreCase))
                ?? Exercises[0];
        }
    }

    private async Task HandleGoalTypeChangedAsync(int typeIndex)
    {
        switch (typeIndex)
        {
            case 0: // ExerciseWeight
                if (SelectedExercise != null)
                {
                    GoalTitle = SelectedExercise.Name;
                    await FetchPrForExerciseAsync(SelectedExercise.Id);
                }
                break;

            case 1: // BodyWeight
                GoalTitle = "Целевой вес тела";
                var latestWeight = await _bodyMeasurementRepo.GetLatestAsync();
                if (latestWeight != null && latestWeight.WeightKg > 0)
                {
                    StartValueText = latestWeight.WeightKg.ToString("F1", CultureInfo.InvariantCulture);
                    CurrentValueText = latestWeight.WeightKg.ToString("F1", CultureInfo.InvariantCulture);
                    TargetValueText = (latestWeight.WeightKg - 3.0).ToString("F1", CultureInfo.InvariantCulture);
                }
                else
                {
                    StartValueText = "80.0";
                    CurrentValueText = "80.0";
                    TargetValueText = "75.0";
                }
                break;

            case 2: // BodyMeasurement
                SelectedBodyPart = "Бицепс";
                GoalTitle = $"Обхват: {SelectedBodyPart}";
                await FetchMeasurementForPartAsync(SelectedBodyPart);
                break;

            case 3: // WorkoutsFrequency
                GoalTitle = "Регулярность тренировок";
                StartValueText = "8";
                CurrentValueText = "0";
                TargetValueText = "16";
                break;
        }
    }

    private async Task FetchPrForExerciseAsync(int exerciseId)
    {
        var maxWeightPr = await _prRepo.GetBestRecordAsync(exerciseId, RecordType.MaxWeight);
        if (maxWeightPr != null && maxWeightPr.Weight > 0)
        {
            StartValueText = maxWeightPr.Weight.ToString("F1", CultureInfo.InvariantCulture);
            CurrentValueText = maxWeightPr.Weight.ToString("F1", CultureInfo.InvariantCulture);
            TargetValueText = (maxWeightPr.Weight + 10.0).ToString("F1", CultureInfo.InvariantCulture);
        }
        else
        {
            StartValueText = "60.0";
            CurrentValueText = "60.0";
            TargetValueText = "70.0";
        }
    }

    private async Task FetchMeasurementForPartAsync(string partName)
    {
        var latest = await _bodyMeasurementRepo.GetLatestAsync();
        double val = 0;
        if (latest != null)
        {
            val = partName switch
            {
                "Бицепс" => latest.BicepsCm ?? 0,
                "Грудь" => latest.ChestCm ?? 0,
                "Талия" => latest.WaistCm ?? 0,
                "Бёдра" => latest.HipsCm ?? 0,
                "Шея" => latest.NeckCm ?? 0,
                "Голень" => latest.CalfCm ?? 0,
                _ => 0
            };
        }

        if (val > 0)
        {
            StartValueText = val.ToString("F1", CultureInfo.InvariantCulture);
            CurrentValueText = val.ToString("F1", CultureInfo.InvariantCulture);
            TargetValueText = (val + 2.0).ToString("F1", CultureInfo.InvariantCulture);
        }
        else
        {
            StartValueText = "35.0";
            CurrentValueText = "35.0";
            TargetValueText = "38.0";
        }
    }

    private async Task LoadExistingGoalAsync(int id)
    {
        var goal = await _goalRepo.GetByIdAsync(id);
        if (goal == null) return;

        Title = "Редактирование цели";
        IsEditing = true;
        GoalTitle = goal.Title;
        GoalTypeIndex = (int)goal.Type;
        StartValueText = goal.StartValue.ToString("F1", CultureInfo.InvariantCulture);
        TargetValueText = goal.TargetValue.ToString("F1", CultureInfo.InvariantCulture);
        CurrentValueText = goal.CurrentValue.ToString("F1", CultureInfo.InvariantCulture);
        HasTargetDate = goal.TargetDate.HasValue;
        TargetDate = goal.TargetDate ?? DateTime.Today.AddMonths(1);
        StatusIndex = (int)goal.Status;
        Notes = goal.Notes ?? "";

        if (goal.Type == GoalType.ExerciseWeight && goal.ExerciseId.HasValue)
        {
            SelectedExercise = Exercises.FirstOrDefault(e => e.Id == goal.ExerciseId.Value);
        }
        else if (goal.Type == GoalType.BodyMeasurement && !string.IsNullOrWhiteSpace(goal.BodyPart))
        {
            SelectedBodyPart = goal.BodyPart;
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(GoalTitle))
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите название цели.", "OK");
            return;
        }

        if (!TryParseNumber(StartValueText, out var startVal) || startVal < 0)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите корректное начальное значение.", "OK");
            return;
        }

        if (!TryParseNumber(TargetValueText, out var targetVal) || targetVal <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите корректное целевое значение (больше 0).", "OK");
            return;
        }

        if (Math.Abs(startVal - targetVal) < 0.0001)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Целевое значение должно отличаться от начального.", "OK");
            return;
        }

        double curVal = startVal;
        if (TryParseNumber(CurrentValueText, out var parsedCur) && parsedCur >= 0)
        {
            curVal = parsedCur;
        }

        Goal goal;
        if (IsEditing)
        {
            var existing = await _goalRepo.GetByIdAsync(_goalId);
            if (existing == null) return;
            goal = existing;
        }
        else
        {
            goal = new Goal();
        }

        goal.Title = GoalTitle.Trim();
        goal.Type = (GoalType)GoalTypeIndex;
        goal.StartValue = startVal;
        goal.TargetValue = targetVal;
        goal.CurrentValue = curVal;
        goal.TargetDate = HasTargetDate ? TargetDate.Date : null;
        goal.Status = (GoalStatus)StatusIndex;
        goal.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();

        if (goal.Type == GoalType.ExerciseWeight)
        {
            if (SelectedExercise == null)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Выберите упражнение из списка.", "OK");
                return;
            }
            goal.ExerciseId = SelectedExercise.Id;
            goal.ExerciseName = SelectedExercise.Name;
            goal.BodyPart = null;
        }
        else if (goal.Type == GoalType.BodyMeasurement)
        {
            goal.ExerciseId = null;
            goal.ExerciseName = null;
            goal.BodyPart = SelectedBodyPart;
        }
        else
        {
            goal.ExerciseId = null;
            goal.ExerciseName = null;
            goal.BodyPart = null;
        }

        if (IsEditing)
        {
            await _goalRepo.UpdateAsync(goal);
        }
        else
        {
            await _goalRepo.AddAsync(goal);
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (!IsEditing) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Удаление цели",
            "Вы действительно хотите удалить эту цель?",
            "Удалить",
            "Отмена");

        if (!confirm) return;

        await _goalRepo.DeleteAsync(_goalId);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private static bool TryParseNumber(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
