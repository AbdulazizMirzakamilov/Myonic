using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Calendar;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Pages;
using Myonic.Services;

namespace Myonic.ViewModels;

public sealed class CalendarDayItem : ObservableObject
{
    private DateTime _date;
    private string _dayName = string.Empty;
    private string _dayNumber = string.Empty;
    private bool _isToday;
    private bool _isSelected;
    private bool _isCurrentMonth = true;
    private bool _hasCompletedWorkout;
    private bool _hasPlannedWorkout;
    private bool _hasMissedWorkout;

    public DateTime Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public string DayName
    {
        get => _dayName;
        set => SetProperty(ref _dayName, value);
    }

    public string DayNumber
    {
        get => _dayNumber;
        set => SetProperty(ref _dayNumber, value);
    }

    public bool IsToday
    {
        get => _isToday;
        set => SetProperty(ref _isToday, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsCurrentMonth
    {
        get => _isCurrentMonth;
        set => SetProperty(ref _isCurrentMonth, value);
    }

    public bool HasCompletedWorkout
    {
        get => _hasCompletedWorkout;
        set
        {
            if (SetProperty(ref _hasCompletedWorkout, value))
            {
                OnPropertyChanged(nameof(ShowPlannedIndicator));
                OnPropertyChanged(nameof(ShowMissedIndicator));
                OnPropertyChanged(nameof(HasAnyWorkout));
            }
        }
    }

    public bool HasPlannedWorkout
    {
        get => _hasPlannedWorkout;
        set
        {
            if (SetProperty(ref _hasPlannedWorkout, value))
            {
                OnPropertyChanged(nameof(ShowPlannedIndicator));
                OnPropertyChanged(nameof(HasAnyWorkout));
            }
        }
    }

    public bool HasMissedWorkout
    {
        get => _hasMissedWorkout;
        set
        {
            if (SetProperty(ref _hasMissedWorkout, value))
            {
                OnPropertyChanged(nameof(ShowMissedIndicator));
                OnPropertyChanged(nameof(HasAnyWorkout));
            }
        }
    }

    public bool ShowPlannedIndicator => HasPlannedWorkout && !HasCompletedWorkout;
    public bool ShowMissedIndicator => HasMissedWorkout && !HasCompletedWorkout;
    public bool HasAnyWorkout => HasCompletedWorkout || ShowPlannedIndicator || ShowMissedIndicator;

    public void NotifyThemeChanged()
    {
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(HasCompletedWorkout));
    }
}

public sealed class CalendarWeekRowItem
{
    public CalendarDayItem Day0 { get; }
    public CalendarDayItem Day1 { get; }
    public CalendarDayItem Day2 { get; }
    public CalendarDayItem Day3 { get; }
    public CalendarDayItem Day4 { get; }
    public CalendarDayItem Day5 { get; }
    public CalendarDayItem Day6 { get; }

    public CalendarWeekRowItem(
        CalendarDayItem d0, CalendarDayItem d1, CalendarDayItem d2,
        CalendarDayItem d3, CalendarDayItem d4, CalendarDayItem d5, CalendarDayItem d6)
    {
        Day0 = d0; Day1 = d1; Day2 = d2;
        Day3 = d3; Day4 = d4; Day5 = d5; Day6 = d6;
    }
}

public partial class ScheduleViewModel : ObservableObject, IDisposable
{
    private readonly ICalendarService _calendarService;
    private readonly IWorkoutSessionRepository _sessionRepo;
    private readonly IWorkoutScheduleRepository _scheduleRepo;
    private readonly IThemeService _themeService;
    private readonly Action<bool> _themeChangedHandler;
    private bool _isDisposed;

    private static readonly CultureInfo RussianCulture = new("ru-RU");

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _currentPeriodDate = DateTime.Today;

    [ObservableProperty]
    private string _monthYearHeader = string.Empty;

    [ObservableProperty]
    private string _weekNumberHeader = string.Empty;

    [ObservableProperty]
    private string _selectedDateFormatted = string.Empty;

    [ObservableProperty]
    private bool _isMonthView = true;

    public bool IsWeekView => !IsMonthView;

    [ObservableProperty]
    private bool _hasWorkouts;

    [ObservableProperty]
    private bool _isRestDay;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<CalendarDayItem> WeekDays { get; } = [];
    public ObservableCollection<CalendarWeekRowItem> MonthWeeks { get; } = [];
    public ObservableCollection<CalendarWorkoutItem> Workouts { get; } = [];

    // Dedicated day items for fixed column grid bindings on SchedulePage
    public CalendarDayItem Day0 { get; } = new();
    public CalendarDayItem Day1 { get; } = new();
    public CalendarDayItem Day2 { get; } = new();
    public CalendarDayItem Day3 { get; } = new();
    public CalendarDayItem Day4 { get; } = new();
    public CalendarDayItem Day5 { get; } = new();
    public CalendarDayItem Day6 { get; } = new();

    public IActiveWorkoutTracker ActiveWorkoutTracker { get; }

    public ScheduleViewModel(
        ICalendarService calendarService,
        IWorkoutSessionRepository sessionRepo,
        IWorkoutScheduleRepository scheduleRepo,
        IThemeService themeService,
        IActiveWorkoutTracker? activeWorkoutTracker = null)
    {
        _calendarService = calendarService;
        _sessionRepo = sessionRepo;
        _scheduleRepo = scheduleRepo;
        _themeService = themeService;
        ActiveWorkoutTracker = activeWorkoutTracker ?? new ActiveWorkoutTracker(sessionRepo);

        WeekDays.Add(Day0);
        WeekDays.Add(Day1);
        WeekDays.Add(Day2);
        WeekDays.Add(Day3);
        WeekDays.Add(Day4);
        WeekDays.Add(Day5);
        WeekDays.Add(Day6);

        _themeChangedHandler = _ =>
        {
            foreach (var d in WeekDays) d.NotifyThemeChanged();
            foreach (var w in MonthWeeks)
            {
                w.Day0.NotifyThemeChanged();
                w.Day1.NotifyThemeChanged();
                w.Day2.NotifyThemeChanged();
                w.Day3.NotifyThemeChanged();
                w.Day4.NotifyThemeChanged();
                w.Day5.NotifyThemeChanged();
                w.Day6.NotifyThemeChanged();
            }
        };
    }


    public void SubscribeThemeEvents()
    {
        _themeService.ThemeChanged -= _themeChangedHandler;
        _themeService.ThemeChanged += _themeChangedHandler;
    }

    public void UnsubscribeThemeEvents()
    {
        _themeService.ThemeChanged -= _themeChangedHandler;
    }

    public Task InitializeAsync() => RefreshCalendarAsync();

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        UnsubscribeThemeEvents();
    }

    partial void OnIsMonthViewChanged(bool value)
    {
        OnPropertyChanged(nameof(IsWeekView));
        UpdateHeaders();
    }

    [RelayCommand]
    public void SwitchToMonthView()
    {
        IsMonthView = true;
    }

    [RelayCommand]
    public void SwitchToWeekView()
    {
        IsMonthView = false;
    }

    [RelayCommand]
    public void ToggleCalendarViewMode()
    {
        IsMonthView = !IsMonthView;
    }

    [RelayCommand]
    public async Task RefreshCalendarAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            await ActiveWorkoutTracker.RefreshAsync();

            var current = CurrentPeriodDate;
            UpdateHeaders();

            // 1. Calculate week bounds (Monday to Sunday)
            var dayOfWeek = (int)current.DayOfWeek;
            var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            var weekStart = current.Date.AddDays(-daysFromMonday);
            var weekEnd = weekStart.AddDays(6);

            // 2. Calculate month grid bounds
            var firstOfMonth = new DateTime(current.Year, current.Month, 1);
            var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

            var firstDow = (int)firstOfMonth.DayOfWeek;
            var daysBefore = firstDow == 0 ? 6 : firstDow - 1;
            var gridStart = firstOfMonth.AddDays(-daysBefore);

            var lastDow = (int)lastOfMonth.DayOfWeek;
            var daysAfter = lastDow == 0 ? 0 : 7 - lastDow;
            var gridEnd = lastOfMonth.AddDays(daysAfter);

            // Fetch indicators for the union range
            var overallStart = weekStart < gridStart ? weekStart : gridStart;
            var overallEnd = weekEnd > gridEnd ? weekEnd : gridEnd;
            var indicators = await _calendarService.GetIndicatorsForRangeAsync(overallStart, overallEnd);

            // 3. Populate 7 days of WeekDays
            var dayNames = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            for (int i = 0; i < 7; i++)
            {
                var d = weekStart.AddDays(i);
                var item = WeekDays[i];

                item.Date = d;
                item.DayName = dayNames[i];
                item.DayNumber = d.Day.ToString();
                item.IsToday = d.Date == DateTime.Today;
                item.IsSelected = d.Date == SelectedDate.Date;
                item.IsCurrentMonth = d.Month == current.Month;

                if (indicators.TryGetValue(d.Date, out var ind))
                {
                    item.HasCompletedWorkout = ind.HasCompleted;
                    item.HasPlannedWorkout = ind.HasPlanned;
                    item.HasMissedWorkout = ind.HasMissed;
                }
                else
                {
                    item.HasCompletedWorkout = false;
                    item.HasPlannedWorkout = false;
                    item.HasMissedWorkout = false;
                }
            }

            // 4. Populate Month Grid (MonthWeeks)
            MonthWeeks.Clear();
            var totalDays = (gridEnd - gridStart).Days + 1;
            var numWeeks = totalDays / 7;
            var curDate = gridStart;

            for (int w = 0; w < numWeeks; w++)
            {
                var rowDays = new CalendarDayItem[7];
                for (int d = 0; d < 7; d++)
                {
                    var isSelected = curDate.Date == SelectedDate.Date;
                    var isToday = curDate.Date == DateTime.Today;
                    var isCurrentMonth = curDate.Month == current.Month;

                    var hasCompleted = false;
                    var hasPlanned = false;
                    var hasMissed = false;

                    if (indicators.TryGetValue(curDate.Date, out var ind))
                    {
                        hasCompleted = ind.HasCompleted;
                        hasPlanned = ind.HasPlanned;
                        hasMissed = ind.HasMissed;
                    }

                    rowDays[d] = new CalendarDayItem
                    {
                        Date = curDate,
                        DayName = dayNames[d],
                        DayNumber = curDate.Day.ToString(),
                        IsToday = isToday,
                        IsSelected = isSelected,
                        IsCurrentMonth = isCurrentMonth,
                        HasCompletedWorkout = hasCompleted,
                        HasPlannedWorkout = hasPlanned,
                        HasMissedWorkout = hasMissed
                    };

                    curDate = curDate.AddDays(1);
                }

                MonthWeeks.Add(new CalendarWeekRowItem(
                    rowDays[0], rowDays[1], rowDays[2], rowDays[3], rowDays[4], rowDays[5], rowDays[6]));
            }

            await LoadWorkoutsForSelectedDateAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateHeaders()
    {
        var target = IsMonthView ? CurrentPeriodDate : SelectedDate;
        var monthName = target.ToString("MMMM", RussianCulture);
        if (!string.IsNullOrEmpty(monthName))
        {
            monthName = char.ToUpper(monthName[0]) + monthName[1..];
        }
        MonthYearHeader = $"{monthName} {target.Year}";

        var cal = RussianCulture.Calendar;
        var weekNum = cal.GetWeekOfYear(target, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        WeekNumberHeader = $"НЕДЕЛЯ {weekNum}";

        var dayName = SelectedDate.ToString("dddd", RussianCulture);
        if (!string.IsNullOrEmpty(dayName))
        {
            dayName = char.ToUpper(dayName[0]) + dayName[1..];
        }
        SelectedDateFormatted = $"{dayName}, {SelectedDate:d MMMM yyyy}";
    }

    [RelayCommand]
    public async Task SelectDayAsync(CalendarDayItem? day)
    {
        if (day == null) return;
        SelectedDate = day.Date;

        // If day belongs to another month, update CurrentPeriodDate
        if (day.Date.Month != CurrentPeriodDate.Month || day.Date.Year != CurrentPeriodDate.Year)
        {
            CurrentPeriodDate = day.Date;
            await RefreshCalendarAsync();
            return;
        }

        UpdateHeaders();

        foreach (var d in WeekDays)
            d.IsSelected = d.Date.Date == SelectedDate.Date;

        foreach (var week in MonthWeeks)
        {
            week.Day0.IsSelected = week.Day0.Date.Date == SelectedDate.Date;
            week.Day1.IsSelected = week.Day1.Date.Date == SelectedDate.Date;
            week.Day2.IsSelected = week.Day2.Date.Date == SelectedDate.Date;
            week.Day3.IsSelected = week.Day3.Date.Date == SelectedDate.Date;
            week.Day4.IsSelected = week.Day4.Date.Date == SelectedDate.Date;
            week.Day5.IsSelected = week.Day5.Date.Date == SelectedDate.Date;
            week.Day6.IsSelected = week.Day6.Date.Date == SelectedDate.Date;
        }

        await LoadWorkoutsForSelectedDateAsync();
    }

    private async Task LoadWorkoutsForSelectedDateAsync()
    {
        var items = await _calendarService.GetWorkoutsForDateAsync(SelectedDate);

        Workouts.Clear();
        foreach (var item in items)
        {
            Workouts.Add(item);
        }

        HasWorkouts = Workouts.Count > 0;
        IsRestDay = !HasWorkouts;
    }

    [RelayCommand]
    public async Task PreviousWeekAsync()
    {
        CurrentPeriodDate = CurrentPeriodDate.AddDays(-7);
        SelectedDate = SelectedDate.AddDays(-7);
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    public async Task NextWeekAsync()
    {
        CurrentPeriodDate = CurrentPeriodDate.AddDays(7);
        SelectedDate = SelectedDate.AddDays(7);
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    public async Task PreviousMonthAsync()
    {
        CurrentPeriodDate = CurrentPeriodDate.AddMonths(-1);
        SelectedDate = new DateTime(CurrentPeriodDate.Year, CurrentPeriodDate.Month, Math.Min(SelectedDate.Day, DateTime.DaysInMonth(CurrentPeriodDate.Year, CurrentPeriodDate.Month)));
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    public async Task NextMonthAsync()
    {
        CurrentPeriodDate = CurrentPeriodDate.AddMonths(1);
        SelectedDate = new DateTime(CurrentPeriodDate.Year, CurrentPeriodDate.Month, Math.Min(SelectedDate.Day, DateTime.DaysInMonth(CurrentPeriodDate.Year, CurrentPeriodDate.Month)));
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    public async Task PreviousPeriodAsync()
    {
        if (IsMonthView)
            await PreviousMonthAsync();
        else
            await PreviousWeekAsync();
    }

    [RelayCommand]
    public async Task NextPeriodAsync()
    {
        if (IsMonthView)
            await NextMonthAsync();
        else
            await NextWeekAsync();
    }

    [RelayCommand]
    public async Task TodayAsync()
    {
        CurrentPeriodDate = DateTime.Today;
        SelectedDate = DateTime.Today;
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    public async Task OpenFullCalendarAsync()
    {
        await Shell.Current.GoToAsync(nameof(CalendarFullPage));
    }

    [RelayCommand]
    public async Task OpenWorkoutDetailAsync(CalendarWorkoutItem? item)
    {
        if (item == null || !item.SessionId.HasValue) return;
        await Shell.Current.GoToAsync($"{nameof(WorkoutSessionDetailPage)}?sessionId={item.SessionId.Value}");
    }

    [RelayCommand]
    public async Task StartPlannedWorkoutAsync(CalendarWorkoutItem? workout)
    {
        if (workout == null || !workout.ProgramWorkoutId.HasValue) return;

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

        var sessionId = await _sessionRepo.StartSessionFromProgramWorkoutAsync(workout.ProgramWorkoutId.Value);
        ActiveWorkoutTracker.NotifySessionStarted(sessionId, workout.Title);
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
        ActiveWorkoutTracker.NotifySessionStarted(sessionId, title);
        await Shell.Current.GoToAsync($"{nameof(ActiveWorkoutPage)}?sessionId={sessionId}");
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

        var titles = allWorkouts.Select(w => $"{w.ProgramName} – {w.WorkoutName}").ToArray();
        var selectedTitle = await Shell.Current.DisplayActionSheetAsync(
            "Выберите тренировку для старта",
            "Отмена",
            null,
            titles);

        if (selectedTitle == null || selectedTitle == "Отмена")
            return;

        var chosen = allWorkouts.FirstOrDefault(w => $"{w.ProgramName} – {w.WorkoutName}" == selectedTitle);
        if (chosen != null)
        {
            var item = new CalendarWorkoutItem
            {
                ProgramWorkoutId = chosen.ProgramWorkoutId,
                Title = chosen.WorkoutName
            };
            await StartPlannedWorkoutAsync(item);
        }
    }
}
