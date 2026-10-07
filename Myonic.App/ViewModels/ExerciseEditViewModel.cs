using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.ViewModels;

public partial class ExerciseEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly IExerciseRepository _repository;
    private Exercise? _exercise;   // null = создаём новое

    public ExerciseEditViewModel(IExerciseRepository repository) => _repository = repository;

    // Порядок совпадает с enum ExerciseType: Compound = 0, Isolation = 1
    public IReadOnlyList<string> TypeOptions { get; } = ["Базовое", "Изолирующее"];

    [ObservableProperty] private string _title = "Новое упражнение";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private int _typeIndex;
    [ObservableProperty] private bool _canDelete;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("id", out var value) || !int.TryParse(value?.ToString(), out var id))
            return;

        _exercise = await _repository.GetByIdAsync(id);
        if (_exercise is null) return;

        Title = "Редактирование";
        Name = _exercise.Name;
        Description = _exercise.Description;
        Notes = _exercise.Notes;
        TypeIndex = (int)_exercise.Type;
        CanDelete = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите название упражнения.", "OK");
            return;
        }

        try
        {
            if (_exercise is null)
            {
                await _repository.AddAsync(new Exercise
                {
                    Name = Name,
                    Description = Description,
                    Notes = Notes,
                    Type = (ExerciseType)TypeIndex
                });
            }
            else
            {
                _exercise.Name = Name;
                _exercise.Description = Description;
                _exercise.Notes = Notes;
                _exercise.Type = (ExerciseType)TypeIndex;
                await _repository.UpdateAsync(_exercise);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (InvalidOperationException ex)   // дубликат названия
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_exercise is null) return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Удалить упражнение", $"Удалить «{_exercise.Name}»?", "Удалить", "Отмена");
        if (!confirmed) return;

        var result = await _repository.RemoveAsync(_exercise.Id);
        if (result == ExerciseRemoval.Archived)
        {
            await Shell.Current.DisplayAlertAsync(
                "Упражнение скрыто",
                "Оно есть в программах или истории, поэтому скрыто из списка. История сохранена.",
                "OK");
        }

        await Shell.Current.GoToAsync("..");
    }
}