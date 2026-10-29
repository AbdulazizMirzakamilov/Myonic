using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Core.Validation;

namespace Myonic.ViewModels;

public partial class ProgramExerciseEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly IProgramExerciseRepository _repository;
    private ProgramExercise? _model;
    private int _workoutId;
    private int _exerciseId;

    [ObservableProperty]
    private string _exerciseName = string.Empty;

    [ObservableProperty]
    private string _setsText = "3";

    [ObservableProperty]
    private string _repsText = "8";

    [ObservableProperty]
    private string _repsMaxText = string.Empty;

    [ObservableProperty]
    private string _weightText = string.Empty;

    [ObservableProperty]
    private string _rpeText = string.Empty;

    [ObservableProperty]
    private string _rpeMaxText = string.Empty;

    [ObservableProperty]
    private string _restText = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private bool _isExisting;

    public ProgramExerciseEditViewModel(IProgramExerciseRepository repository)
    {
        _repository = repository;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) && int.TryParse(idObj.ToString(), out var id) && id > 0)
        {
            _model = await _repository.GetByIdAsync(id);
            if (_model != null)
            {
                IsExisting = true;
                _workoutId = _model.ProgramWorkoutId;
                _exerciseId = _model.ExerciseId;

                SetsText = _model.Sets.ToString();
                RepsText = _model.Reps.ToString();
                RepsMaxText = _model.RepsMax?.ToString() ?? string.Empty;
                WeightText = _model.Weight?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
                RpeText = _model.Rpe?.ToString("0.#", CultureInfo.InvariantCulture) ?? string.Empty;
                RpeMaxText = _model.RpeMax?.ToString("0.#", CultureInfo.InvariantCulture) ?? string.Empty;
                RestText = _model.RestSeconds?.ToString() ?? string.Empty;
                Notes = _model.Notes ?? string.Empty;
            }
        }
        else
        {
            IsExisting = false;
            if (query.TryGetValue("workoutId", out var wObj) && int.TryParse(wObj.ToString(), out var wId))
                _workoutId = wId;
            if (query.TryGetValue("exerciseId", out var eObj) && int.TryParse(eObj.ToString(), out var eId))
                _exerciseId = eId;
        }

        if (query.TryGetValue("exerciseName", out var nameObj) && nameObj != null)
        {
            ExerciseName = nameObj.ToString()!;
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (!int.TryParse(SetsText, out var sets) || sets <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Количество подходов должно быть числом больше 0.", "ОК");
            return;
        }

        if (!int.TryParse(RepsText, out var reps) || reps <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Количество повторений должно быть числом больше 0.", "ОК");
            return;
        }

        int? repsMax = null;
        if (!string.IsNullOrWhiteSpace(RepsMaxText))
        {
            if (!int.TryParse(RepsMaxText, out var rm) || rm <= reps)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Максимальное число повторений должно быть строго больше начального.", "ОК");
                return;
            }
            repsMax = rm;
        }

        double? weight = null;
        if (!string.IsNullOrWhiteSpace(WeightText))
        {
            var cleanW = WeightText.Replace(',', '.').Trim();
            if (!double.TryParse(cleanW, NumberStyles.Float, CultureInfo.InvariantCulture, out var w) || w < 0)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Вес должен быть неотрицательным числом.", "ОК");
                return;
            }
            weight = w;
        }

        double? rpe = null;
        if (!string.IsNullOrWhiteSpace(RpeText))
        {
            var cleanRpe = RpeText.Replace(',', '.').Trim();
            if (!double.TryParse(cleanRpe, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRpe) ||
                !ProgramExerciseValidator.IsValidRpeValue(parsedRpe))
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "RPE должен быть от 5.5 до 10 с шагом 0.5 (например, 7.5 или 8).", "ОК");
                return;
            }
            rpe = parsedRpe;
        }

        double? rpeMax = null;
        if (!string.IsNullOrWhiteSpace(RpeMaxText))
        {
            if (!rpe.HasValue)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Нельзя указать верхний RPE без начального.", "ОК");
                return;
            }

            var cleanRpeMax = RpeMaxText.Replace(',', '.').Trim();
            if (!double.TryParse(cleanRpeMax, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRpeMax) ||
                !ProgramExerciseValidator.IsValidRpeValue(parsedRpeMax) ||
                parsedRpeMax <= rpe.Value)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Верхний RPE должен быть от 5.5 до 10 с шагом 0.5 и строго больше начального.", "ОК");
                return;
            }
            rpeMax = parsedRpeMax;
        }

        int? rest = null;
        if (!string.IsNullOrWhiteSpace(RestText))
        {
            if (!int.TryParse(RestText, out var r) || r < 0)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Время отдыха должно быть неотрицательным числом секунд.", "ОК");
                return;
            }
            rest = r;
        }

        var item = _model ?? new ProgramExercise
        {
            ProgramWorkoutId = _workoutId,
            ExerciseId = _exerciseId
        };

        item.Sets = sets;
        item.Reps = reps;
        item.RepsMax = repsMax;
        item.Weight = weight;
        item.Rpe = rpe;
        item.RpeMax = rpeMax;
        item.RestSeconds = rest;
        item.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();

        try
        {
            if (IsExisting)
            {
                await _repository.UpdateAsync(item);
            }
            else
            {
                await _repository.AddAsync(item);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", ex.Message, "ОК");
        }
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (_model == null) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Удаление", "Убрать упражнение из тренировки?", "Да", "Нет");
        if (!confirm) return;

        await _repository.DeleteAsync(_model.Id);
        await Shell.Current.GoToAsync("..");
    }
}
