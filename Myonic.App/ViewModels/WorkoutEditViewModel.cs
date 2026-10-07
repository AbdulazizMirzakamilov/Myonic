using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.ViewModels;

public partial class WorkoutEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly ITrainingProgramRepository _repository;
    private ProgramWorkout? _workout;

    public WorkoutEditViewModel(ITrainingProgramRepository repository) => _repository = repository;

    public IReadOnlyList<string> DayOptions => WeekdayHelper.Options;

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