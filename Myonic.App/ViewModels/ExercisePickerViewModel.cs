using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public partial class ExercisePickerViewModel : ObservableObject, IQueryAttributable
{
    private readonly IExerciseRepository _repository;
    private int _workoutId;

    public ExercisePickerViewModel(IExerciseRepository repository) => _repository = repository;

    public ObservableCollection<ExerciseListItem> Exercises { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => LoadCommand.Execute(null);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("workoutId", out var value) && int.TryParse(value?.ToString(), out var id))
            _workoutId = id;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var items = await _repository.GetAllAsync(SearchText);

        Exercises.Clear();
        foreach (var e in items)
            Exercises.Add(new ExerciseListItem(e.Id, e.Name, e.Type.ToDisplayText()));
    }

    // "../" закрывает экран выбора и сразу открывает экран параметров,
    // поэтому кнопка "Назад" потом ведёт прямо в тренировку
    [RelayCommand]
    private Task SelectAsync(ExerciseListItem item) =>
        Shell.Current.GoToAsync(
            $"../{nameof(ProgramExerciseEditPage)}?workoutId={_workoutId}&exerciseId={item.Id}");
}