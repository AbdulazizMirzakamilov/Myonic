using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public record ExerciseListItem(int Id, string Name, string TypeText);

internal static class ExerciseTypeExtensions
{
    public static string ToDisplayText(this ExerciseType type) => type switch
    {
        ExerciseType.Compound => "Базовое",
        ExerciseType.Isolation => "Изолирующее",
        _ => type.ToString()
    };
}

public partial class ExercisesViewModel : ObservableObject
{
    private readonly IExerciseRepository _repository;

    public ExercisesViewModel(IExerciseRepository repository) => _repository = repository;

    public ObservableCollection<ExerciseListItem> Exercises { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => LoadCommand.Execute(null);

    [RelayCommand]
    private async Task LoadAsync()
    {
        var items = await _repository.GetAllAsync(SearchText);

        Exercises.Clear();
        foreach (var e in items)
            Exercises.Add(new ExerciseListItem(e.Id, e.Name, e.Type.ToDisplayText()));
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(nameof(ExerciseEditPage));

    [RelayCommand]
    private Task OpenAsync(ExerciseListItem item) =>
        Shell.Current.GoToAsync($"{nameof(ExerciseEditPage)}?id={item.Id}");
}