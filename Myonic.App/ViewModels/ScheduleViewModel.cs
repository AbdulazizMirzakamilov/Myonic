using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public sealed class CalendarDayItem : ObservableObject
{
    private bool _isSelected;
    private bool _hasCompletedWorkout;
    private bool _hasPlannedWorkout;

    public DateTime Date { get; init; }
    public string DayName { get; init; } = string.Empty;
    public string DayNumber { get; init; } = string.Empty;
    public bool IsToday { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool HasCompletedWorkout
    {
        get => _hasCompletedWorkout;
        set => SetProperty(ref _hasCompletedWorkout, value);
    }

    public bool HasPlannedWorkout
    {
        get => _hasPlannedWorkout;
        set => SetProperty(ref _hasPlannedWorkout, value);
    }
}

public sealed class DayCompletedSessionItem
{
    public int SessionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string TimeText { get; init; } = string.Empty;
    public string DurationText { get; init; } = string.Empty;
    public int ExercisesCount { get; init; }
    public int SetsCount { get; init; }
    public List<DayCompletedExerciseItem> Exercises { get; init; } = [];
}

public sealed class DayCompletedExerciseItem
{
    public string ExerciseName { get; init; } = string.Empty;
    public List<string> SetsSummary { get; init; } = [];
}

public partial class ScheduleViewModel : ObservableObject
{
    private readonly IWorkoutSessionRepository _sessionRepo;
    private readonly IWorkoutScheduleRepository _scheduleRepo;

    private static readonly CultureInfo RussianCulture = new("ru-RU");

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _monthYearHeader = string.Empty;

    [ObservableProperty]
    private string _weekNumberHeader = string.Empty;

    [ObservableProperty]
    private string _selectedDateFormatted = string.Empty;

    [ObservableProperty]
    private ActiveSessionSummary? _activeSession;

    [ObservableProperty]
    private bool _hasActiveSession;

    [ObservableProperty]
    private bool _hasCompletedWorkouts;

    [ObservableProperty]
    private bool _hasPlannedWorkouts;

    [ObservableProperty]
    private bool _isRestDay;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<CalendarDayItem> WeekDays { get; } = [];
    public ObservableCollection<DayCompletedSessionItem> CompletedSessions { get; } = [];
    public ObservableCollection<PlannedWorkoutSummary> PlannedWorkouts { get; } = [];

    public ScheduleViewModel(
        IWorkoutSessionRepository sessionRepo,
        IWorkoutScheduleRepository scheduleRepo)
    {
        _sessionRepo = sessionRepo;
        _scheduleRepo = scheduleRepo;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        SelectedDate = DateTime.Today;
        await RefreshWeekAsync();
    }

    [RelayCommand]
    public async Task RefreshWeekAsync()
    {
        IsLoading = true;
        try
        {
            // Active session banner
            ActiveSession = await _sessionRepo.GetActiveSessionSummaryAsync();
            HasActiveSession = ActiveSession != null;

            // Determine Monday of current week
            var current = SelectedDate;
            var diff = (7 + (current.DayOfWeek - DayOfWeek.Monday)) % 7;
            var monday = current.AddDays(-diff).Date;
            var sunday = monday.AddDays(6).Date;

            // Headers
            MonthYearHeader = monday.ToString("MMMM yyyy", RussianCulture);
            var calendar = RussianCulture.Calendar;
            var weekNum = calendar.GetWeekOfYear(monday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            WeekNumberHeader = $"Неделя {weekNum}";

            // Completed session dates for the week
            var completedDates = await _sessionRepo.GetCompletedSessionDatesAsync(monday, sunday.AddDays(1));
            var completedSet = new HashSet<DateTime>(completedDates.Select(d => d.Date));

            WeekDays.Clear();
            var dayNames = new[] { "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ", "ВС" };

            for (int i = 0; i < 7; i++)
            {
                var dayDate = monday.AddDays(i);
                var isSelected = dayDate.Date == SelectedDate.Date;
                var isToday = dayDate.Date == DateTime.Today;
                var hasCompleted = completedSet.Contains(dayDate.Date);

                // Check if planned workout exists for day
                var planned = await _scheduleRepo.GetPlannedWorkoutsForDateAsync(dayDate);

                WeekDays.Add(new CalendarDayItem
                {
                    Date = dayDate,
                    DayName = dayNames[i],
                    DayNumber = dayDate.Day.ToString(),
                    IsToday = isToday,
                    IsSelected = isSelected,
                    HasCompletedWorkout = hasCompleted,
                    HasPlannedWorkout = planned.Count > 0
                });
            }

            await LoadSelectedDateDetailsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectDayAsync(CalendarDayItem? day)
    {
        if (day == null) return;

        SelectedDate = day.Date;

        foreach (var d in WeekDays)
            d.IsSelected = d.Date.Date == SelectedDate.Date;

        await LoadSelectedDateDetailsAsync();
    }

    [RelayCommand]
    public async Task PreviousWeekAsync()
    {
        SelectedDate = SelectedDate.AddDays(-7);
        await RefreshWeekAsync();
    }

    [RelayCommand]
    public async Task NextWeekAsync()
    {
        SelectedDate = SelectedDate.AddDays(7);
        await RefreshWeekAsync();
    }

    [RelayCommand]
    public async Task TodayAsync()
    {
        SelectedDate = DateTime.Today;
        await RefreshWeekAsync();
    }

    private async Task LoadSelectedDateDetailsAsync()
    {
        SelectedDateFormatted = SelectedDate.ToString("dddd, d MMMM yyyy", RussianCulture);

        // 1. Load completed workouts for selected date
        CompletedSessions.Clear();
        var sessions = await _sessionRepo.GetSessionsForDateAsync(SelectedDate);

        foreach (var s in sessions)
        {
            var duration = (s.FinishedAt ?? DateTime.UtcNow) - s.StartedAt;
            var durationText = duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours} ч {duration.Minutes} мин"
                : $"{duration.Minutes} мин";

            var timeText = s.FinishedAt.HasValue
                ? $"{s.StartedAt.ToLocalTime():HH:mm} – {s.FinishedAt.Value.ToLocalTime():HH:mm}"
                : $"{s.StartedAt.ToLocalTime():HH:mm}";

            var totalSets = s.Exercises.Sum(e => e.Sets.Count);

            var exItems = new List<DayCompletedExerciseItem>();
            foreach (var ex in s.Exercises)
            {
                var setLines = new List<string>();
                foreach (var set in ex.Sets)
                {
                    var line = $"Сет {set.SetNumber}: {set.Weight:0.##} кг × {set.Reps}";
                    if (set.Rpe.HasValue)
                        line += $" @ {set.Rpe.Value:0.#}";
                    setLines.Add(line);
                }

                exItems.Add(new DayCompletedExerciseItem
                {
                    ExerciseName = ex.ExerciseName,
                    SetsSummary = setLines
                });
            }

            CompletedSessions.Add(new DayCompletedSessionItem
            {
                SessionId = s.Id,
                Title = s.Title,
                TimeText = timeText,
                DurationText = durationText,
                ExercisesCount = s.Exercises.Count,
                SetsCount = totalSets,
                Exercises = exItems
            });
        }

        HasCompletedWorkouts = CompletedSessions.Count > 0;

        // 2. Load planned workouts for selected date
        PlannedWorkouts.Clear();
        var planned = await _scheduleRepo.GetPlannedWorkoutsForDateAsync(SelectedDate);
        foreach (var p in planned)
        {
            PlannedWorkouts.Add(p);
        }

        HasPlannedWorkouts = PlannedWorkouts.Count > 0;
        IsRestDay = !HasCompletedWorkouts && !HasPlannedWorkouts;
    }

    [RelayCommand]
    public async Task StartPlannedWorkoutAsync(PlannedWorkoutSummary? workout)
    {
        if (workout == null) return;

        // Check if there's already an active session
        var active = await _sessionRepo.GetActiveSessionSummaryAsync();
        if (active != null)
        {
            var proceed = await Shell.Current.DisplayAlertAsync(
                "Тренировка уже идет",
                $"Сейчас активна тренировка «{active.Title}». Открыть её?",
                "Открыть", "Отмена");
            if (proceed)
            {
                await Shell.Current.GoToAsync($"{nameof(ActiveWorkoutPage)}?sessionId={active.Id}");
            }
            return;
        }

        var sessionId = await _sessionRepo.StartSessionFromProgramWorkoutAsync(workout.ProgramWorkoutId);
        await Shell.Current.GoToAsync($"{nameof(ActiveWorkoutPage)}?sessionId={sessionId}");
    }

    [RelayCommand]
    public async Task StartFreeWorkoutAsync()
    {
        var active = await _sessionRepo.GetActiveSessionSummaryAsync();
        if (active != null)
        {
            var proceed = await Shell.Current.DisplayAlertAsync(
                "Тренировка уже идет",
                $"Сейчас активна тренировка «{active.Title}». Открыть её?",
                "Открыть", "Отмена");
            if (proceed)
            {
                await Shell.Current.GoToAsync($"{nameof(ActiveWorkoutPage)}?sessionId={active.Id}");
            }
            return;
        }

        var name = await Shell.Current.DisplayPromptAsync(
            "Свободная тренировка",
            "Введите название тренировки (или оставьте по умолчанию):",
            "Начать", "Отмена",
            initialValue: "Свободная тренировка",
            maxLength: 50);

        if (name == null) return; // User cancelled

        var title = string.IsNullOrWhiteSpace(name) ? "Свободная тренировка" : name.Trim();
        var sessionId = await _sessionRepo.StartEmptySessionAsync(title);
        await Shell.Current.GoToAsync($"{nameof(ActiveWorkoutPage)}?sessionId={sessionId}");
    }

    [RelayCommand]
    public async Task ResumeActiveSessionAsync()
    {
        if (ActiveSession == null) return;
        await Shell.Current.GoToAsync($"{nameof(ActiveWorkoutPage)}?sessionId={ActiveSession.Id}");
    }

    [RelayCommand]
    public async Task ChooseProgramWorkoutAsync()
    {
        var allWorkouts = await _scheduleRepo.GetAllAvailableProgramWorkoutsAsync();
        if (allWorkouts.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Программы не найдены", "Сначала создайте программу тренировок во вкладке «Программы».", "OK");
            return;
        }

        var titles = allWorkouts.Select(w => $"{w.ProgramName} — {w.WorkoutName}").ToArray();
        var selectedTitle = await Shell.Current.DisplayActionSheetAsync(
            "Выберите тренировку для старта",
            "Отмена",
            null,
            titles);

        if (selectedTitle == null || selectedTitle == "Отмена")
            return;

        var chosen = allWorkouts.FirstOrDefault(w => $"{w.ProgramName} — {w.WorkoutName}" == selectedTitle);
        if (chosen != null)
        {
            await StartPlannedWorkoutAsync(chosen);
        }
    }
}
