using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Core.Validation;
using Myonic.Pages;

namespace Myonic.ViewModels;

public sealed class SetEntryItem
{
    public int Id { get; init; }
    public int SetNumber { get; set; }
    public double Weight { get; init; }
    public int Reps { get; init; }
    public double? Rpe { get; init; }
    public string SummaryText
    {
        get
        {
            var text = $"Сет {SetNumber}: {Weight:0.##} кг × {Reps}";
            if (Rpe.HasValue)
                text += $" @ {Rpe.Value:0.#}";
            return text;
        }
    }
}

public partial class ActiveExerciseViewModel : ObservableObject
{
    private readonly IWorkoutSessionRepository _repository;

    public int WorkoutExerciseId { get; init; }
    public int Order { get; init; }
    public string ExerciseName { get; init; } = string.Empty;
    public string PlanText { get; init; } = string.Empty;

    public ObservableCollection<SetEntryItem> Sets { get; } = [];

    [ObservableProperty]
    private string _nextWeightText = string.Empty;

    [ObservableProperty]
    private string _nextRepsText = string.Empty;

    [ObservableProperty]
    private string _nextRpeText = string.Empty;

    public ActiveExerciseViewModel(
        IWorkoutSessionRepository repository,
        SessionExerciseDetail detail)
    {
        _repository = repository;
        WorkoutExerciseId = detail.WorkoutExerciseId;
        Order = detail.Order;
        ExerciseName = detail.ExerciseName;
        PlanText = detail.PlanText;

        // Prefill default next weight and reps from plan
        if (detail.PlannedWeight.HasValue && detail.PlannedWeight.Value > 0)
            NextWeightText = detail.PlannedWeight.Value.ToString("0.##", CultureInfo.InvariantCulture);

        if (detail.PlannedReps.HasValue && detail.PlannedReps.Value > 0)
            NextRepsText = detail.PlannedReps.Value.ToString();

        if (detail.PlannedRpe.HasValue)
            NextRpeText = detail.PlannedRpe.Value.ToString("0.#", CultureInfo.InvariantCulture);

        foreach (var s in detail.Sets)
        {
            Sets.Add(new SetEntryItem
            {
                Id = s.Id,
                SetNumber = s.SetNumber,
                Weight = s.Weight,
                Reps = s.Reps,
                Rpe = s.Rpe
            });
        }
    }

    [RelayCommand]
    public async Task AddSetAsync()
    {
        var cleanWeight = (NextWeightText ?? string.Empty).Replace(',', '.').Trim();
        if (!double.TryParse(cleanWeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight) || weight < 0)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите корректный вес подхода (>= 0).", "OK");
            return;
        }

        var cleanReps = (NextRepsText ?? string.Empty).Trim();
        if (!int.TryParse(cleanReps, out var reps) || reps <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите количество повторений (> 0).", "OK");
            return;
        }

        double? rpe = null;
        if (!string.IsNullOrWhiteSpace(NextRpeText))
        {
            var cleanRpe = NextRpeText.Replace(',', '.').Trim();
            if (!double.TryParse(cleanRpe, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRpe) ||
                !ProgramExerciseValidator.IsValidRpeValue(parsedRpe))
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "RPE должен быть от 5.5 до 10 с шагом 0.5 (например, 7.5 или 8).", "OK");
                return;
            }
            rpe = parsedRpe;
        }

        try
        {
            var created = await _repository.AddSetAsync(WorkoutExerciseId, weight, reps, rpe);
            Sets.Add(new SetEntryItem
            {
                Id = created.Id,
                SetNumber = created.SetNumber,
                Weight = created.Weight,
                Reps = created.Reps,
                Rpe = created.Rpe
            });

            // Keep weight and reps for the next set, clear RPE
            NextRpeText = string.Empty;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    [RelayCommand]
    public async Task DeleteSetAsync(SetEntryItem item)
    {
        if (item == null) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Удаление", $"Удалить подход №{item.SetNumber}?", "Да", "Нет");
        if (!confirm) return;

        await _repository.DeleteSetAsync(item.Id);
        Sets.Remove(item);

        // Re-index remaining set numbers locally
        for (int i = 0; i < Sets.Count; i++)
        {
            Sets[i].SetNumber = i + 1;
        }
    }
}

public partial class ActiveWorkoutViewModel : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutSessionRepository _repository;
    private IDispatcherTimer? _timer;
    private DateTime _startedAt;
    private int _sessionId;

    [ObservableProperty]
    private string _title = "Тренировка";

    [ObservableProperty]
    private string _durationText = "00:00:00";

    public ObservableCollection<ActiveExerciseViewModel> Exercises { get; } = [];

    public ActiveWorkoutViewModel(IWorkoutSessionRepository repository)
    {
        _repository = repository;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("sessionId", out var sIdObj) || !int.TryParse(sIdObj?.ToString(), out var sId) || sId <= 0)
            return;

        _sessionId = sId;
        await LoadSessionAsync();
        StartTimer();
    }

    public async Task LoadSessionAsync()
    {
        var detail = await _repository.GetSessionDetailAsync(_sessionId);
        if (detail == null) return;

        Title = detail.Title;
        _startedAt = detail.StartedAt;

        Exercises.Clear();
        foreach (var ex in detail.Exercises)
        {
            Exercises.Add(new ActiveExerciseViewModel(_repository, ex));
        }

        UpdateDuration();
    }

    private void StartTimer()
    {
        _timer?.Stop();
        if (Application.Current != null)
        {
            _timer = Application.Current.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateDuration();
            _timer.Start();
        }
    }

    public void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    private void UpdateDuration()
    {
        var elapsed = DateTime.UtcNow - _startedAt;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
        DurationText = elapsed.ToString(@"hh\:mm\:ss");
    }

    [RelayCommand]
    public async Task AddExerciseAsync()
    {
        var picker = IPlatformApplication.Current?.Services.GetService<ExercisePickerPage>()
                     ?? Application.Current?.Handler?.MauiContext?.Services.GetService<ExercisePickerPage>();
        if (picker == null) return;

        var vm = (ExercisePickerViewModel)picker.BindingContext;
        vm.ExerciseSelected += async exercise =>
        {
            var detail = await _repository.AddExerciseToSessionAsync(_sessionId, exercise.Id);
            Exercises.Add(new ActiveExerciseViewModel(_repository, detail));
        };

        await Shell.Current.Navigation.PushModalAsync(picker);
    }

    [RelayCommand]
    public async Task RemoveExerciseAsync(ActiveExerciseViewModel item)
    {
        if (item == null) return;
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Удаление упражнения",
            $"Удалить упражнение «{item.ExerciseName}» из текущей тренировки?",
            "Да", "Нет");
        if (!confirm) return;

        await _repository.DeleteExerciseFromSessionAsync(item.WorkoutExerciseId);
        Exercises.Remove(item);
    }

    [RelayCommand]
    public async Task FinishSessionAsync()
    {
        var totalSets = Exercises.Sum(e => e.Sets.Count);

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Завершение тренировки",
            $"Завершить тренировку?\nВыполнено подходов: {totalSets}\nВремя: {DurationText}",
            "Завершить", "Продолжить");

        if (!confirm) return;

        StopTimer();
        await _repository.FinishSessionAsync(_sessionId);
        await Shell.Current.DisplayAlertAsync("Отличная работа!", "Тренировка успешно сохранена в истории.", "OK");
        await Shell.Current.GoToAsync("//SchedulePage");
    }

    [RelayCommand]
    public async Task DiscardSessionAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Отмена тренировки",
            "Вы уверены, что хотите отменить тренировку? Все записанные подходы будут удалены.",
            "Отменить тренировку", "Вернуться");

        if (!confirm) return;

        StopTimer();
        await _repository.DiscardSessionAsync(_sessionId);
        await Shell.Current.GoToAsync("//SchedulePage");
    }
}
