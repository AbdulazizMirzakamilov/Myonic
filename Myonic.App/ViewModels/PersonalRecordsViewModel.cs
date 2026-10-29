using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public sealed class ExerciseGroupedRecords
{
    public int ExerciseId { get; init; }
    public string ExerciseName { get; init; } = string.Empty;
    public List<PersonalRecordItem> Records { get; init; } = [];
}

public partial class PersonalRecordsViewModel : ObservableObject
{
    private readonly IPersonalRecordRepository _prRepo;
    private readonly IExerciseRepository _exerciseRepo;

    public ObservableCollection<ExerciseGroupedRecords> GroupedRecords { get; } = [];

    [ObservableProperty]
    private bool _hasRecords;

    [ObservableProperty]
    private bool _hasNoRecords = true;

    [ObservableProperty]
    private bool _isLoading;

    public PersonalRecordsViewModel(
        IPersonalRecordRepository prRepo,
        IExerciseRepository exerciseRepo)
    {
        _prRepo = prRepo;
        _exerciseRepo = exerciseRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var records = await _prRepo.GetAllAsync();
            GroupedRecords.Clear();

            var grouped = records
                .GroupBy(r => new { r.ExerciseId, r.ExerciseName })
                .Select(g => new ExerciseGroupedRecords
                {
                    ExerciseId = g.Key.ExerciseId,
                    ExerciseName = g.Key.ExerciseName,
                    Records = g.OrderBy(r => r.Type).ThenByDescending(r => r.AchievedAt).ToList()
                })
                .OrderBy(g => g.ExerciseName);

            foreach (var g in grouped)
            {
                GroupedRecords.Add(g);
            }

            HasRecords = GroupedRecords.Count > 0;
            HasNoRecords = !HasRecords;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddManualRecordAsync()
    {
        var exercises = await _exerciseRepo.GetAllAsync();
        if (exercises.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Внимание", "Сначала создайте хотя бы одно упражнение.", "OK");
            return;
        }

        var names = exercises.Select(e => e.Name).ToArray();
        var selectedName = await Shell.Current.DisplayActionSheetAsync(
            "Выберите упражнение для рекорда",
            "Отмена",
            null,
            names);

        if (string.IsNullOrEmpty(selectedName) || selectedName == "Отмена") return;

        var exercise = exercises.First(e => e.Name == selectedName);

        var weightStr = await Shell.Current.DisplayPromptAsync(
            "Вес рекорда",
            $"Введите максимальный вес для «{exercise.Name}» (в кг):",
            "Далее",
            "Отмена",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(weightStr)) return;

        var cleanWeight = weightStr.Replace(',', '.').Trim();
        if (!double.TryParse(cleanWeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight) || weight <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите корректный положительный вес.", "OK");
            return;
        }

        var repsStr = await Shell.Current.DisplayPromptAsync(
            "Количество повторений",
            "Введите количество повторений (например, 1 или 5):",
            "Сохранить",
            "Отмена",
            initialValue: "1",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(repsStr)) return;

        if (!int.TryParse(repsStr.Trim(), out var reps) || reps <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", "Введите корректное число повторений (> 0).", "OK");
            return;
        }

        var recordType = reps switch
        {
            1 => RecordType.OneRepMax,
            5 => RecordType.FiveRepMax,
            _ => RecordType.MaxWeight
        };

        await _prRepo.AddManualRecordAsync(exercise.Id, recordType, weight, reps, DateTime.UtcNow);
        await LoadAsync();

        await Shell.Current.DisplayAlertAsync("Успешно", $"Личный рекорд для «{exercise.Name}» ({weight} кг × {reps}) сохранён!", "OK");
    }

    [RelayCommand]
    public async Task OpenExerciseDetailAsync(ExerciseGroupedRecords group)
    {
        if (group == null) return;
        await Shell.Current.GoToAsync($"{nameof(ExerciseDetailPage)}?id={group.ExerciseId}");
    }

    [RelayCommand]
    public async Task DeleteRecordAsync(PersonalRecordItem record)
    {
        if (record == null) return;

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Удаление рекорда",
            $"Удалить рекорд {record.Weight} кг ({record.TypeDisplayName}) для «{record.ExerciseName}»?",
            "Удалить",
            "Отмена");

        if (!confirm) return;

        await _prRepo.DeleteRecordAsync(record.Id);
        await LoadAsync();
    }
}
