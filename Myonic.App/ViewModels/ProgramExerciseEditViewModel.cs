using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using System.Globalization;

namespace Myonic.ViewModels;

public partial class ProgramExerciseEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly IProgramExerciseRepository _repository;
    private readonly IExerciseRepository _exerciseRepository;

    private ProgramExercise? _item;   // null = добавляем новое
    private int _workoutId;
    private int _exerciseId;

    public ProgramExerciseEditViewModel(
        IProgramExerciseRepository repository,
        IExerciseRepository exerciseRepository)
    {
        _repository = repository;
        _exerciseRepository = exerciseRepository;
    }

    [ObservableProperty] private string _exerciseName = string.Empty;
    [ObservableProperty] private string _setsText = string.Empty;
    [ObservableProperty] private string _repsText = string.Empty;
    [ObservableProperty] private string _repsMaxText = string.Empty;
    [ObservableProperty] private string _weightText = string.Empty;
    [ObservableProperty] private string _rpeText = string.Empty;
    [ObservableProperty] private string _rpeMaxText = string.Empty;
    [ObservableProperty] private string _restText = string.Empty;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private bool _isExisting;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idValue) && int.TryParse(idValue?.ToString(), out var id))
        {
            _item = await _repository.GetAsync(id);
            if (_item is null) return;

            _workoutId = _item.ProgramWorkoutId;
            _exerciseId = _item.ExerciseId;
            SetsText = _item.Sets.ToString();
            RepsText = _item.Reps.ToString();
            RepsMaxText = _item.RepsMax?.ToString() ?? string.Empty;
            WeightText = _item.Weight?.ToString("0.##") ?? string.Empty;
            RpeText = _item.Rpe?.ToString("0.#") ?? string.Empty;
            RpeMaxText = _item.RpeMax?.ToString("0.#") ?? string.Empty;
            RestText = _item.RestSeconds?.ToString() ?? string.Empty;
            Notes = _item.Notes;
            IsExisting = true;
        }
        else if (query.TryGetValue("workoutId", out var w) && int.TryParse(w?.ToString(), out var workoutId)
                 && query.TryGetValue("exerciseId", out var e) && int.TryParse(e?.ToString(), out var exerciseId))
        {
            _workoutId = workoutId;
            _exerciseId = exerciseId;
        }
        else
        {
            return;
        }

        var exercise = await _exerciseRepository.GetByIdAsync(_exerciseId);
        ExerciseName = exercise?.Name ?? "—";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!int.TryParse(SetsText, out var sets) || sets <= 0)
        {
            await ShowErrorAsync("Подходы: целое число больше нуля.");
            return;
        }

        if (!int.TryParse(RepsText, out var reps) || reps <= 0)
        {
            await ShowErrorAsync("Повторения: целое число больше нуля.");
            return;
        }

        int? repsMax = null;
        if (!string.IsNullOrWhiteSpace(RepsMaxText))
        {
            if (!int.TryParse(RepsMaxText, out var rm) || rm <= reps)
            {
                await ShowErrorAsync("Максимальное число повторений должно быть целым числом строго больше минимального.");
                return;
            }
            repsMax = rm;
        }

        double? weight = null;
        if (!string.IsNullOrWhiteSpace(WeightText))
        {
            if (!TryParseDouble(WeightText, out var w) || w < 0)
            {
                await ShowErrorAsync("Вес: число, не меньше нуля.");
                return;
            }
            weight = w;
        }

        double? rpe = null;
        if (!string.IsNullOrWhiteSpace(RpeText))
        {
            if (!TryParseDouble(RpeText, out var r) || !IsValidRpe(r))
            {
                await ShowErrorAsync("RPE: число от 5.5 до 10 с шагом 0.5 (например: 6, 6.5, 7, 7.5...).");
                return;
            }
            rpe = r;
        }

        double? rpeMax = null;
        if (!string.IsNullOrWhiteSpace(RpeMaxText))
        {
            if (rpe is null)
            {
                await ShowErrorAsync("Нельзя указать верхний RPE без нижнего.");
                return;
            }
            if (!TryParseDouble(RpeMaxText, out var rm) || !IsValidRpe(rm))
            {
                await ShowErrorAsync("Верхний RPE: число от 5.5 до 10 с шагом 0.5.");
                return;
            }
            if (rm <= rpe.Value)
            {
                await ShowErrorAsync("Верхний RPE должен быть строго больше нижнего.");
                return;
            }
            rpeMax = rm;
        }

        int? rest = null;
        if (!string.IsNullOrWhiteSpace(RestText))
        {
            if (!int.TryParse(RestText, out var s) || s < 0)
            {
                await ShowErrorAsync("Отдых: целое число секунд, не меньше нуля.");
                return;
            }
            rest = s;
        }

        try
        {
            if (_item is null)
            {
                await _repository.AddAsync(new ProgramExercise
                {
                    ProgramWorkoutId = _workoutId,
                    ExerciseId = _exerciseId,
                    Sets = sets,
                    Reps = reps,
                    RepsMax = repsMax,
                    Weight = weight,
                    Rpe = rpe,
                    RpeMax = rpeMax,
                    RestSeconds = rest,
                    Notes = Notes
                });
            }
            else
            {
                _item.Sets = sets;
                _item.Reps = reps;
                _item.RepsMax = repsMax;
                _item.Weight = weight;
                _item.Rpe = rpe;
                _item.RpeMax = rpeMax;
                _item.RestSeconds = rest;
                _item.Notes = Notes;
                await _repository.UpdateAsync(_item);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (ArgumentException ex)
        {
            await ShowErrorAsync(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_item is null) return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Убрать упражнение", $"Убрать «{ExerciseName}» из тренировки?", "Убрать", "Отмена");
        if (!confirmed) return;

        await _repository.DeleteAsync(_item.Id);
        await Shell.Current.GoToAsync("..");
    }

    private static bool IsValidRpe(double rpe)
    {
        if (rpe is < 5.5 or > 10.0) return false;
        var doubled = rpe * 2.0;
        return Math.Abs(doubled - Math.Round(doubled)) < 1e-6;
    }

    // Принимаем и запятую, и точку
    private static bool TryParseDouble(string? text, out double value) =>
        double.TryParse(text?.Trim().Replace(',', '.'),
            NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static Task ShowErrorAsync(string message) =>
        Shell.Current.DisplayAlertAsync("Ошибка", message, "OK");
}
