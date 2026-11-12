using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Pages;
using Myonic.Services;

namespace Myonic.ViewModels;

public record ExerciseListItem(
    int Id,
    string Name,
    string TypeText,
    bool IsCompound,
    string MuscleGroupText,
    bool HasMuscleGroup,
    bool HasNoMuscleGroup,
    string? SecondaryMuscleGroupText = null);

public partial class MuscleGroupFilterItem : ObservableObject
{
    public MuscleGroup Group { get; init; }
    public string DisplayName { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}

internal static class ExerciseTypeExtensions
{
    public static string ToDisplayText(this ExerciseType type) => type switch
    {
        ExerciseType.Compound => "Базовое",
        ExerciseType.Isolation => "Изолирующее",
        _ => type.ToString()
    };
}

public partial class ExercisesViewModel : ObservableObject, IHasActiveWorkoutTracker, IDisposable
{
    private const int PageSize = 30;
    private readonly IExerciseRepository _repository;
    private readonly List<ExerciseListItem> _allFilteredExercises = [];
    private int _loadedCount;
    private CancellationTokenSource? _searchCts;
    public IActiveWorkoutTracker? ActiveWorkoutTracker { get; }

    public ExercisesViewModel(
        IExerciseRepository repository,
        IActiveWorkoutTracker? activeWorkoutTracker = null)
    {
        _repository = repository;
        ActiveWorkoutTracker = activeWorkoutTracker;
        InitializeMuscleGroupFilters();
    }

    public ObservableCollection<ExerciseListItem> Exercises { get; } = [];
    public ObservableCollection<MuscleGroupFilterItem> MuscleGroupFilters { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _filterIndex; // 0: Все, 1: Базовые, 2: Изолирующие

    [ObservableProperty]
    private MuscleGroup _selectedMuscleGroup = MuscleGroup.None;

    [ObservableProperty]
    private bool _hasExercises;

    [ObservableProperty]
    private bool _hasNoExercises;

    [ObservableProperty]
    private string _countText = string.Empty;

    public bool IsAllFilter => FilterIndex == 0;
    public bool IsCompoundFilter => FilterIndex == 1;
    public bool IsIsolationFilter => FilterIndex == 2;

    private void InitializeMuscleGroupFilters()
    {
        MuscleGroupFilters.Clear();
        MuscleGroupFilters.Add(new MuscleGroupFilterItem { Group = MuscleGroup.None, DisplayName = "Все мышцы", IsSelected = true });
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

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, token);
                if (!token.IsCancellationRequested)
                {
                    await MainThread.InvokeOnMainThreadAsync(LoadAsync);
                }
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }

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
    public void SelectMuscleGroup(MuscleGroupFilterItem filterItem)
    {
        if (filterItem == null) return;

        // Если повторно нажимаем на уже выбранную категорию (кроме "Все мышцы"), сбрасываем на "Все"
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

        _ = LoadAsync();
    }

    [RelayCommand]
    public void LoadMoreExercises()
    {
        if (_loadedCount >= _allFilteredExercises.Count) return;

        var countToTake = Math.Min(PageSize, _allFilteredExercises.Count - _loadedCount);
        for (int i = 0; i < countToTake; i++)
        {
            Exercises.Add(_allFilteredExercises[_loadedCount + i]);
        }
        _loadedCount += countToTake;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        ExerciseType? typeFilter = FilterIndex switch
        {
            1 => ExerciseType.Compound,
            2 => ExerciseType.Isolation,
            _ => null
        };

        var items = await _repository.GetAllAsync(SearchText, SelectedMuscleGroup, typeFilter);

        _allFilteredExercises.Clear();
        foreach (var e in items)
        {
            var hasMuscleGroup = e.MuscleGroup != MuscleGroup.None;
            var muscleGroupText = hasMuscleGroup ? e.MuscleGroup.ToDisplayText() : string.Empty;
            var secondaryText = e.SecondaryMuscleGroup.HasValue ? e.SecondaryMuscleGroup.Value.ToDisplayText() : null;

            _allFilteredExercises.Add(new ExerciseListItem(
                e.Id,
                e.Name,
                e.Type.ToDisplayText(),
                e.Type == ExerciseType.Compound,
                muscleGroupText,
                hasMuscleGroup,
                !hasMuscleGroup,
                secondaryText));
        }

        Exercises.Clear();
        _loadedCount = Math.Min(PageSize, _allFilteredExercises.Count);
        for (int i = 0; i < _loadedCount; i++)
        {
            Exercises.Add(_allFilteredExercises[i]);
        }

        HasExercises = _allFilteredExercises.Count > 0;
        HasNoExercises = !HasExercises;
        CountText = $"Всего: {_allFilteredExercises.Count}";
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(nameof(ExerciseEditPage));

    [RelayCommand]
    private Task OpenRecordsAsync() => Shell.Current.GoToAsync(nameof(PersonalRecordsPage));

    [RelayCommand]
    private Task OpenAsync(ExerciseListItem item) =>
        Shell.Current.GoToAsync($"{nameof(ExerciseDetailPage)}?id={item.Id}");
}
