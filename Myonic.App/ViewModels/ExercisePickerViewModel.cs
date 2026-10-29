using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.ViewModels;

public partial class ExercisePickerViewModel : ObservableObject
{
    private readonly IExerciseRepository _repository;
    private IReadOnlyList<Exercise> _allExercises = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<Exercise> FilteredExercises { get; } = [];

    public event Action<Exercise>? ExerciseSelected;

    public ExercisePickerViewModel(IExerciseRepository repository)
    {
        _repository = repository;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        _allExercises = await _repository.GetAllAsync();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        var filtered = _allExercises
            .Where(e => string.IsNullOrWhiteSpace(query) ||
                        e.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        FilteredExercises.Clear();
        foreach (var ex in filtered)
            FilteredExercises.Add(ex);
    }

    [RelayCommand]
    public async Task SelectExerciseAsync(Exercise exercise)
    {
        if (exercise == null) return;
        ExerciseSelected?.Invoke(exercise);
        await Shell.Current.Navigation.PopModalAsync();
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        await Shell.Current.Navigation.PopModalAsync();
    }
}
