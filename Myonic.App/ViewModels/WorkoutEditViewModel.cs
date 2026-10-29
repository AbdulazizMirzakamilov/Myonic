using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Formatting;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public sealed class PlannedExerciseItem
{
    public int Id { get; init; }
    public int ExerciseId { get; init; }
    public int Order { get; init; }
    public string OrderText => Order.ToString();
    public string Title { get; init; } = string.Empty;
    public string PlanText { get; init; } = string.Empty;
}

public partial class WorkoutEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly ITrainingProgramRepository _programRepository;
    private readonly IProgramExerciseRepository _exerciseRepository;
    private readonly IServiceProvider _serviceProvider;
    private ProgramWorkout? _workout;

    public WorkoutEditViewModel(
        ITrainingProgramRepository programRepository,
        IProgramExerciseRepository exerciseRepository,
        IServiceProvider serviceProvider)
    {
        _programRepository = programRepository;
        _exerciseRepository = exerciseRepository;
        _serviceProvider = serviceProvider;
    }

    public IReadOnlyList<string> DayOptions => WeekdayHelper.Options;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _dayIndex;

    public ObservableCollection<PlannedExerciseItem> Exercises { get; } = [];

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("id", out var value) || !int.TryParse(value?.ToString(), out var id))
            return;

        _workout = await _programRepository.GetWorkoutAsync(id);
        if (_workout is null) return;

        Name = _workout.Name;
        DayIndex = WeekdayHelper.ToIndex(_workout.DefaultDayOfWeek);

        await LoadExercisesAsync();
    }

    public async Task LoadExercisesAsync()
    {
        if (_workout is null) return;

        var items = await _exerciseRepository.GetByWorkoutIdAsync(_workout.Id);
        Exercises.Clear();

        foreach (var ex in items)
        {
            var planText = PlanTextFormatter.Format(
                ex.Sets,
                ex.Reps,
                ex.RepsMax,
                ex.Weight,
                ex.Rpe,
                ex.RpeMax);

            Exercises.Add(new PlannedExerciseItem
            {
                Id = ex.Id,
                ExerciseId = ex.ExerciseId,
                Order = ex.Order,
                Title = ex.ExerciseName,
                PlanText = planText
            });
        }
    }

    [RelayCommand]
    private async Task AddExerciseAsync()
    {
        if (_workout is null) return;

        var pickerPage = _serviceProvider.GetRequiredService<ExercisePickerPage>();
        if (pickerPage.BindingContext is ExercisePickerViewModel pickerVm)
        {
            pickerVm.ExerciseSelected += async (selectedExercise) =>
            {
                await Shell.Current.GoToAsync(
                    nameof(ProgramExerciseEditPage),
                    new Dictionary<string, object>
                    {
                        ["workoutId"] = _workout.Id,
                        ["exerciseId"] = selectedExercise.Id,
                        ["exerciseName"] = selectedExercise.Name
                    });
            };
        }

        await Shell.Current.Navigation.PushModalAsync(pickerPage);
    }

    [RelayCommand]
    private async Task OpenExerciseAsync(PlannedExerciseItem item)
    {
        if (item is null) return;

        await Shell.Current.GoToAsync(
            nameof(ProgramExerciseEditPage),
            new Dictionary<string, object>
            {
                ["id"] = item.Id,
                ["exerciseName"] = item.Title
            });
    }

    [RelayCommand]
    private async Task MoveUpAsync(PlannedExerciseItem item)
    {
        if (item is null) return;
        await _exerciseRepository.MoveUpAsync(item.Id);
        await LoadExercisesAsync();
    }

    [RelayCommand]
    private async Task MoveDownAsync(PlannedExerciseItem item)
    {
        if (item is null) return;
        await _exerciseRepository.MoveDownAsync(item.Id);
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
        await _programRepository.UpdateWorkoutAsync(_workout);
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

        await _programRepository.DeleteWorkoutAsync(_workout.Id);
        await Shell.Current.GoToAsync("..");
    }
}
