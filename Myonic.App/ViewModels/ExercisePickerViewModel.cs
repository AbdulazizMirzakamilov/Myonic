using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.ViewModels;

public sealed class ExercisePickerDisplayItem
{
    public Exercise Exercise { get; init; } = null!;
    public string Name => Exercise.Name;
    public string MuscleGroupText => Exercise.MuscleGroup != MuscleGroup.None ? Exercise.MuscleGroup.ToDisplayText() : string.Empty;
    public bool HasMuscleGroup => Exercise.MuscleGroup != MuscleGroup.None;
    public string TypeText => Exercise.Type == ExerciseType.Compound ? "Базовое" : "Изолирующее";
}

public partial class ExercisePickerViewModel : ObservableObject
{
    private readonly IExerciseRepository _repository;
    private IReadOnlyList<Exercise> _allExercises = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private MuscleGroup _selectedMuscleGroup = MuscleGroup.None;

    public ObservableCollection<ExercisePickerDisplayItem> FilteredExercises { get; } = [];
    public ObservableCollection<MuscleGroupFilterItem> MuscleGroupFilters { get; } = [];

    public Func<Exercise, Task>? ExerciseSelectedAsync { get; set; }
    public event Action<Exercise>? ExerciseSelected;

    public ExercisePickerViewModel(IExerciseRepository repository)
    {
        _repository = repository;
        InitializeMuscleGroupFilters();
    }

    private void InitializeMuscleGroupFilters()
    {
        MuscleGroupFilters.Clear();
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.None, DisplayName = "Все", IsSelected = true });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Chest, DisplayName = "Грудь" });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Back, DisplayName = "Спина" });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Shoulders, DisplayName = "Плечи" });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Biceps, DisplayName = "Бицепс" });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Triceps, DisplayName = "Трицепс" });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Forearms, DisplayName = "Предплечья" });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Quadriceps, DisplayName = "Квадрицепсы" });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Hamstrings, DisplayName = "Бёдра (задняя)" });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Glutes, DisplayName = "Ягодицы" });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Calves, DisplayName = "Икры" });
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.Abs, DisplayName = "Пресс" });
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        _allExercises = await _repository.GetAllAsync();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    public void SelectMuscleGroup(MuscleGroupFilterItem filterItem)
    {
        if (filterItem == null) return;

        if (filterItem.Group != MuscleGroup.None && SelectedMuscleGroup == filterItem.Group)
        {
            SelectedMuscleGroup = MuscleGroup.None;
        }
        else
        {
            SelectedMuscleGroup = filterItem.Group;
        }

        foreach (var item in MuscleGroupFilters)
        {
            item.IsSelected = item.Group == SelectedMuscleGroup;
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        var filtered = _allExercises
            .Where(e => (string.IsNullOrWhiteSpace(query) ||
                         e.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)) &&
                        (SelectedMuscleGroup == MuscleGroup.None ||
                         e.MuscleGroup == SelectedMuscleGroup ||
                         e.SecondaryMuscleGroup == SelectedMuscleGroup))
            .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        FilteredExercises.Clear();
        foreach (var ex in filtered)
            FilteredExercises.Add(new ExercisePickerDisplayItem { Exercise = ex });
    }

    [RelayCommand]
    public async Task SelectExerciseAsync(ExercisePickerDisplayItem item)
    {
        if (item?.Exercise == null) return;

        var exercise = item.Exercise;
        var callbackAsync = ExerciseSelectedAsync;
        var callback = ExerciseSelected;

        await Shell.Current.Navigation.PopModalAsync();

        if (callbackAsync != null)
        {
            await callbackAsync.Invoke(exercise);
        }
        else
        {
            callback?.Invoke(exercise);
        }
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        await Shell.Current.Navigation.PopModalAsync();
    }
}
