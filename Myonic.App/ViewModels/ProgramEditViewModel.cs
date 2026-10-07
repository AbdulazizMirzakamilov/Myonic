using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Pages;
using System.Collections.ObjectModel;

namespace Myonic.ViewModels;

public record WorkoutListItem(int Id, string Name, string DayText);

public partial class ProgramEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly ITrainingProgramRepository _repository;
    private TrainingProgram? _program;   // null = программа ещё не создана

    public ProgramEditViewModel(ITrainingProgramRepository repository) => _repository = repository;

    public ObservableCollection<WorkoutListItem> Workouts { get; } = [];

    [ObservableProperty] private string _title = "Новая программа";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private bool _isExisting;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("id", out var value) || !int.TryParse(value?.ToString(), out var id))
            return;

        _program = await _repository.GetProgramAsync(id);
        if (_program is null) return;

        Title = "Программа";
        Name = _program.Name;
        Description = _program.Description;
        IsExisting = true;

        await LoadWorkoutsAsync();
    }

    [RelayCommand]
    private async Task LoadWorkoutsAsync()
    {
        if (_program is null) return;

        var items = await _repository.GetWorkoutsAsync(_program.Id);

        Workouts.Clear();
        foreach (var w in items)
            Workouts.Add(new WorkoutListItem(w.Id, w.Name, WeekdayText(w.DefaultDayOfWeek)));
    }

    private static string WeekdayText(DayOfWeek? day) =>
        day is null ? "День не задан" : WeekdayHelper.ToText(day);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите название программы.", "OK");
            return;
        }

        try
        {
            if (_program is null)
            {
                // Новая программа: создаём и остаёмся на экране, чтобы добавить тренировки
                var id = await _repository.AddProgramAsync(
                    new TrainingProgram { Name = Name, Description = Description });

                _program = await _repository.GetProgramAsync(id);
                Title = "Программа";
                Name = _program!.Name;
                IsExisting = true;
            }
            else
            {
                _program.Name = Name;
                _program.Description = Description;
                await _repository.UpdateProgramAsync(_program);
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (InvalidOperationException ex)   // дубликат названия
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task AddWorkoutAsync()
    {
        if (_program is null) return;

        var name = await Shell.Current.DisplayPromptAsync(
            "Новая тренировка", "Название, например «Грудь + трицепс»", "Добавить", "Отмена");
        if (string.IsNullOrWhiteSpace(name)) return;

        await _repository.AddWorkoutAsync(new ProgramWorkout { ProgramId = _program.Id, Name = name });
        await LoadWorkoutsAsync();
    }

    [RelayCommand]
    private Task OpenWorkoutAsync(WorkoutListItem item) =>
        Shell.Current.GoToAsync($"{nameof(WorkoutEditPage)}?id={item.Id}");

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_program is null) return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Удалить программу",
            $"Удалить «{_program.Name}» вместе с тренировками и расписанием? Выполненные тренировки в истории сохранятся.",
            "Удалить", "Отмена");
        if (!confirmed) return;

        await _repository.DeleteProgramAsync(_program.Id);
        await Shell.Current.GoToAsync("..");
    }
}