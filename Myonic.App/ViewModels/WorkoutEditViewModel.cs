using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public record PlannedExerciseItem(int Id, string Title, string PlanText);

public partial class WorkoutEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly ITrainingProgramRepository _repository;
    private readonly IProgramExerciseRepository _exerciseRepository;
    private ProgramWorkout? _workout;

    public WorkoutEditViewModel(
        ITrainingProgramRepository repository,
        IProgramExerciseRepository exerciseRepository)
    {
        _repository = repository;
        _exerciseRepository = exerciseRepository;
    }

    public IReadOnlyList<string> DayOptions => WeekdayHelper.Options;

    public ObservableCollection<PlannedExerciseItem> Exercises { get; } = [];

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _dayIndex;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("id", out var value) || !int.TryParse(value?.ToString(), out var id))
            return;

        _workout = await _repository.GetWorkoutAsync(id);
        if (_workout is null) return;

        Name = _workout.Name;
        DayIndex = WeekdayHelper.ToIndex(_workout.DefaultDayOfWeek);

        await LoadExercisesAsync();
    }

    [RelayCommand]
    private async Task LoadExercisesAsync()
    {
        if (_workout is null) return;

        var items = await _exerciseRepository.GetByWorkoutAsync(_workout.Id);

        Exercises.Clear();
        for (var i = 0; i < items.Count; i++)
        {
            var e = items[i];
            Exercises.Add(new PlannedExerciseItem(
                e.Id,
                $"{i + 1}. {e.ExerciseName}",
                PlanTextFormatter.Format(e.Sets, e.Reps, e.Weight, e.Rpe, e.RestSeconds)));
        }
    }

    [RelayCommand]
    private Task AddExerciseAsync()
    {
        if (_workout is null) return Task.CompletedTask;
        return Shell.Current.GoToAsync($"{nameof(ExercisePickerPage)}?workoutId={_workout.Id}");
    }

    [RelayCommand]
    private Task OpenExerciseAsync(PlannedExerciseItem item) =>
        Shell.Current.GoToAsync($"{nameof(ProgramExerciseEditPage)}?id={item.Id}");

    [RelayCommand]
    private async Task MoveUpAsync(PlannedExerciseItem item)
    {
        await _exerciseRepository.MoveAsync(item.Id, -1);
        await LoadExercisesAsync();
    }

    [RelayCommand]
    private async Task MoveDownAsync(PlannedExerciseItem item)
    {
        await _exerciseRepository.MoveAsync(item.Id, +1);
        await LoadExercisesAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_workout is null) return;

        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите название тренировки.", "OK");
            return;
        }

        _workout.Name = Name;
        _workout.DefaultDayOfWeek = WeekdayHelper.FromIndex(DayIndex);
        await _repository.UpdateWorkoutAsync(_workout);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_workout is null) return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Удалить тренировку",
            $"Удалить «{_workout.Name}» из программы? Запланированные тренировки тоже удалятся, история сохранится.",
            "Удалить", "Отмена");
        if (!confirmed) return;

        await _repository.DeleteWorkoutAsync(_workout.Id);
        await Shell.Current.GoToAsync("..");
    }
}