using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public record ExerciseListItem(int Id, string Name, string TypeText, bool IsCompound = false);

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

    [ObservableProperty]
    private int _filterIndex; // 0: Все, 1: Базовые, 2: Изолирующие

    [ObservableProperty]
    private bool _hasExercises;

    [ObservableProperty]
    private bool _hasNoExercises;

    [ObservableProperty]
    private string _countText = string.Empty;

    public bool IsAllFilter => FilterIndex == 0;
    public bool IsCompoundFilter => FilterIndex == 1;
    public bool IsIsolationFilter => FilterIndex == 2;

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    public void SelectFilter(string indexStr)
    {
        if (int.TryParse(indexStr, out var idx) && idx >= 0 && idx <= 2)
        {
            FilterIndex = idx;
            OnPropertyChanged(nameof(IsAllFilter));
            OnPropertyChanged(nameof(IsCompoundFilter));
            OnPropertyChanged(nameof(IsIsolationFilter));
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var items = await _repository.GetAllAsync(SearchText);

        if (FilterIndex == 1)
        {
            items = items.Where(x => x.Type == ExerciseType.Compound).ToList();
        }
        else if (FilterIndex == 2)
        {
            items = items.Where(x => x.Type == ExerciseType.Isolation).ToList();
        }

        Exercises.Clear();
        foreach (var e in items)
        {
            Exercises.Add(new ExerciseListItem(e.Id, e.Name, e.Type.ToDisplayText(), e.Type == ExerciseType.Compound));
        }

        HasExercises = Exercises.Count > 0;
        HasNoExercises = !HasExercises;
        CountText = $"Всего: {Exercises.Count}";
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(nameof(ExerciseEditPage));

    [RelayCommand]
    private Task OpenRecordsAsync() => Shell.Current.GoToAsync(nameof(PersonalRecordsPage));

    [RelayCommand]
    private Task OpenAsync(ExerciseListItem item) =>
        Shell.Current.GoToAsync($"{nameof(ExerciseDetailPage)}?id={item.Id}");
}
