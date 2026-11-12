using System.Collections.Concurrent;
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
                OnPropertyChanged(nameof(ShowMissedIndicator));
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
                OnPropertyChanged(nameof(ShowPlannedIndicator));
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
    private bool _isMonthView = false;

    public bool IsWeekView => !IsMonthView;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasWorkouts;

    [ObservableProperty]
    private bool _isRestDay;

    public IActiveWorkoutTracker ActiveWorkoutTracker { get; }

    public ObservableCollection<CalendarDayItem> WeekDays { get; } = [];

    public ObservableCollection<CalendarWeekRowItem> MonthWeeks { get; } = [];

    public ObservableCollection<CalendarWorkoutItem> Workouts { get; } = [];

    public CalendarDayItem Day0 => WeekDays[0];
    public CalendarDayItem Day1 => WeekDays[1];
    public CalendarDayItem Day2 => WeekDays[2];
    public CalendarDayItem Day3 => WeekDays[3];
    public CalendarDayItem Day4 => WeekDays[4];
    public CalendarDayItem Day5 => WeekDays[5];
    public CalendarDayItem Day6 => WeekDays[6];

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

        for (int i = 0; i < 7; i++)
        {
            WeekDays.Add(new CalendarDayItem());
        }

        for (int i = 0; i < 6; i++)
        {
            MonthWeeks.Add(new CalendarWeekRowItem(
                new CalendarDayItem(), new CalendarDayItem(), new CalendarDayItem(),
                new CalendarDayItem(), new CalendarDayItem(), new CalendarDayItem(),
                new CalendarDayItem()));
        }

        UpdateHeaders();

        _themeChangedHandler = _ =>
        {
            MainThread.BeginInvokeOnMainThread(NotifyAllThemeElements);
        };
        _themeService.ThemeChanged += _themeChangedHandler;
    }

    public void NotifyAllThemeElements()
    {
        OnPropertyChanged(nameof(IsWeekView));
        OnPropertyChanged(nameof(IsMonthView));

        foreach (var d in WeekDays)
        {
            d.NotifyThemeChanged();
        }

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

        OnPropertyChanged(nameof(Day0));
        OnPropertyChanged(nameof(Day1));
        OnPropertyChanged(nameof(Day2));
        OnPropertyChanged(nameof(Day3));
        OnPropertyChanged(nameof(Day4));
        OnPropertyChanged(nameof(Day5));
        OnPropertyChanged(nameof(Day6));
        OnPropertyChanged(nameof(MonthWeeks));
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
        _preloadCts?.Cancel();
        _preloadCts?.Dispose();
        UnsubscribeThemeEvents();
    }

    partial void OnIsMonthViewChanged(bool value)
    {
        OnPropertyChanged(nameof(IsWeekView));
        UpdateHeaders();
    }

    [RelayCommand]
    public async Task SwitchToMonthViewAsync()
    {
        if (IsMonthView) return;
        IsMonthView = true;
        CurrentPeriodDate = SelectedDate;
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    public async Task SwitchToWeekViewAsync()
    {
        if (!IsMonthView) return;
        IsMonthView = false;
        CurrentPeriodDate = SelectedDate;
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    public async Task ToggleCalendarViewModeAsync()
    {
        IsMonthView = !IsMonthView;
        CurrentPeriodDate = SelectedDate;
        await RefreshCalendarAsync();
    }

    private readonly ConcurrentDictionary<DateTime, CalendarDayIndicators> _indicatorsCache = new();
    private CancellationTokenSource? _preloadCts;
    private int _refreshCalendarRequestId;
    private int _loadWorkoutsRequestId;

    public void InvalidateCalendarCache()
    {
        _indicatorsCache.Clear();
    }

    [RelayCommand]
    public async Task RefreshCalendarAsync()
    {
        var requestId = Interlocked.Increment(ref _refreshCalendarRequestId);

        IsLoading = true;
        try
        {
            await ActiveWorkoutTracker.RefreshAsync();
            if (requestId != _refreshCalendarRequestId) return;

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

            var overallStart = weekStart < gridStart ? weekStart : gridStart;
            var overallEnd = weekEnd > gridEnd ? weekEnd : gridEnd;

            // Check if entire requested range is available in in-memory cache
            bool allCached = true;
            for (var d = overallStart.Date; d <= overallEnd.Date; d = d.AddDays(1))
            {
                if (!_indicatorsCache.ContainsKey(d))
                {
                    allCached = false;
                    break;
                }
            }

            if (allCached)
            {
                // Instant zero-latency render from pre-cached memory
                ApplyIndicatorsToView(current, weekStart, gridStart, gridEnd, _indicatorsCache);
            }

            // Fetch fresh indicators for union range from service
            var indicators = await _calendarService.GetIndicatorsForRangeAsync(overallStart, overallEnd);
            if (requestId != _refreshCalendarRequestId) return;

            foreach (var kvp in indicators)
            {
                _indicatorsCache[kvp.Key] = kvp.Value;
            }

            ApplyIndicatorsToView(current, weekStart, gridStart, gridEnd, indicators);

            // Background pre-cache of adjacent months (current ± 1 month)
            TriggerPreloadAdjacentMonths(current);

            await LoadWorkoutsForSelectedDateAsync();
        }
        finally
        {
            if (requestId == _refreshCalendarRequestId)
            {
                IsLoading = false;
            }
        }
    }

    private void ApplyIndicatorsToView(
        DateTime current,
        DateTime weekStart,
        DateTime gridStart,
        DateTime gridEnd,
        IReadOnlyDictionary<DateTime, CalendarDayIndicators> indicators)
    {
        var dayNames = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
        for (int i = 0; i < 7; i++)
        {
            var d = weekStart.AddDays(i);
            var item = WeekDays[i];
            UpdateDayItem(item, d, dayNames[i], current.Month, indicators);
        }

        OnPropertyChanged(nameof(Day0));
        OnPropertyChanged(nameof(Day1));
        OnPropertyChanged(nameof(Day2));
        OnPropertyChanged(nameof(Day3));
        OnPropertyChanged(nameof(Day4));
        OnPropertyChanged(nameof(Day5));
        OnPropertyChanged(nameof(Day6));

        var totalDays = (gridEnd - gridStart).Days + 1;
        var numWeeks = totalDays / 7;
        var curDate = gridStart;

        while (MonthWeeks.Count < numWeeks)
        {
            MonthWeeks.Add(new CalendarWeekRowItem(
                new CalendarDayItem(), new CalendarDayItem(), new CalendarDayItem(),
                new CalendarDayItem(), new CalendarDayItem(), new CalendarDayItem(),
                new CalendarDayItem()));
        }
        while (MonthWeeks.Count > numWeeks)
        {
            MonthWeeks.RemoveAt(MonthWeeks.Count - 1);
        }

        for (int w = 0; w < numWeeks; w++)
        {
            var row = MonthWeeks[w];
            UpdateDayItem(row.Day0, curDate, dayNames[0], current.Month, indicators); curDate = curDate.AddDays(1);
            UpdateDayItem(row.Day1, curDate, dayNames[1], current.Month, indicators); curDate = curDate.AddDays(1);
            UpdateDayItem(row.Day2, curDate, dayNames[2], current.Month, indicators); curDate = curDate.AddDays(1);
            UpdateDayItem(row.Day3, curDate, dayNames[3], current.Month, indicators); curDate = curDate.AddDays(1);
            UpdateDayItem(row.Day4, curDate, dayNames[4], current.Month, indicators); curDate = curDate.AddDays(1);
            UpdateDayItem(row.Day5, curDate, dayNames[5], current.Month, indicators); curDate = curDate.AddDays(1);
            UpdateDayItem(row.Day6, curDate, dayNames[6], current.Month, indicators); curDate = curDate.AddDays(1);
        }
    }

    private void TriggerPreloadAdjacentMonths(DateTime centerDate)
    {
        _preloadCts?.Cancel();
        _preloadCts?.Dispose();
        _preloadCts = new CancellationTokenSource();
        var token = _preloadCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                // Range covering center - 1 month to center + 1 month
                var prevMonth = centerDate.AddMonths(-1);
                var nextMonth = centerDate.AddMonths(1);

                var start = new DateTime(prevMonth.Year, prevMonth.Month, 1).AddDays(-7);
                var lastOfNext = new DateTime(nextMonth.Year, nextMonth.Month, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                var end = lastOfNext.AddDays(7);

                var adjacentIndicators = await _calendarService.GetIndicatorsForRangeAsync(start, end);
                if (token.IsCancellationRequested) return;

                foreach (var kvp in adjacentIndicators)
                {
                    _indicatorsCache[kvp.Key] = kvp.Value;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScheduleViewModel] Preload adjacent months failed: {ex.Message}");
            }
        }, token);
    }

    private void UpdateDayItem(
        CalendarDayItem item,
        DateTime date,
        string dayName,
        int currentMonth,
        IReadOnlyDictionary<DateTime, CalendarDayIndicators> indicators)
    {
        item.Date = date;
        item.DayName = dayName;
        item.DayNumber = date.Day.ToString();
        item.IsToday = date.Date == DateTime.Today;
        item.IsSelected = date.Date == SelectedDate.Date;
        item.IsCurrentMonth = date.Month == currentMonth;

        if (indicators.TryGetValue(date.Date, out var ind))
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
        var oldDate = SelectedDate.Date;
        var newDate = day.Date.Date;

        SelectedDate = day.Date;

        // If day belongs to another month, update CurrentPeriodDate
        if (day.Date.Month != CurrentPeriodDate.Month || day.Date.Year != CurrentPeriodDate.Year)
        {
            CurrentPeriodDate = day.Date;
            await RefreshCalendarAsync();
            return;
        }

        UpdateHeaders();

        if (oldDate != newDate)
        {
            // Fine-grained selection update: only touch the two items whose IsSelected changed
            foreach (var d in WeekDays)
            {
                if (d.Date.Date == oldDate) d.IsSelected = false;
                else if (d.Date.Date == newDate) d.IsSelected = true;
            }

            foreach (var week in MonthWeeks)
            {
                UpdateDaySelection(week.Day0, oldDate, newDate);
                UpdateDaySelection(week.Day1, oldDate, newDate);
                UpdateDaySelection(week.Day2, oldDate, newDate);
                UpdateDaySelection(week.Day3, oldDate, newDate);
                UpdateDaySelection(week.Day4, oldDate, newDate);
                UpdateDaySelection(week.Day5, oldDate, newDate);
                UpdateDaySelection(week.Day6, oldDate, newDate);
            }
        }

        await LoadWorkoutsForSelectedDateAsync();
    }

    private static void UpdateDaySelection(CalendarDayItem item, DateTime oldDate, DateTime newDate)
    {
        if (item.Date.Date == oldDate) item.IsSelected = false;
        else if (item.Date.Date == newDate) item.IsSelected = true;
    }

    private async Task LoadWorkoutsForSelectedDateAsync()
    {
        var targetDate = SelectedDate.Date;
        var requestId = Interlocked.Increment(ref _loadWorkoutsRequestId);

        var items = await _calendarService.GetWorkoutsForDateAsync(targetDate);
        if (requestId != _loadWorkoutsRequestId) return;

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
        SelectedDate = new DateTime(
            CurrentPeriodDate.Year,
            CurrentPeriodDate.Month,
            Math.Min(SelectedDate.Day, DateTime.DaysInMonth(CurrentPeriodDate.Year, CurrentPeriodDate.Month)));
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    public async Task NextMonthAsync()
    {
        CurrentPeriodDate = CurrentPeriodDate.AddMonths(1);
        SelectedDate = new DateTime(
            CurrentPeriodDate.Year,
            CurrentPeriodDate.Month,
            Math.Min(SelectedDate.Day, DateTime.DaysInMonth(CurrentPeriodDate.Year, CurrentPeriodDate.Month)));
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

        var sessionId = await _sessionRepo.StartSessionFromProgramWorkoutAsync(workout.ProgramWorkoutId.Value, workout.ScheduledWorkoutId);
        ActiveWorkoutTracker.NotifySessionStarted(sessionId, workout.Title);
        await Shell.Current.GoToAsync($"{nameof(ActiveWorkoutPage)}?sessionId={sessionId}");
    }

    public event Action<CalendarWorkoutItem>? RequestRescheduleDateSelection;

    [RelayCommand]
    public async Task RescheduleWorkoutAsync(CalendarWorkoutItem? workout)
    {
        if (workout == null || !workout.ProgramWorkoutId.HasValue || !workout.ScheduledDate.HasValue)
            return;

        var currentScheduled = workout.ScheduledDate.Value.Date;
        var tomorrow = DateTime.Today.AddDays(1);
        var dayAfterTomorrow = DateTime.Today.AddDays(2);
        var nextWeek = currentScheduled.AddDays(7);

        var optTomorrow = $"Завтра ({tomorrow:d MMMM})";
        var optDayAfter = $"Через 2 дня ({dayAfterTomorrow:d MMMM})";
        var optNextWeek = $"Через неделю ({nextWeek:d MMMM})";
        const string optCustom = "📅 Выбрать точную дату...";

        var choice = await Shell.Current.DisplayActionSheetAsync(
            $"Перенести «{workout.Title}»",
            "Отмена",
            null,
            optTomorrow,
            optDayAfter,
            optNextWeek,
            optCustom);

        if (string.IsNullOrEmpty(choice) || choice == "Отмена")
            return;

        if (choice == optTomorrow)
        {
            await ExecuteRescheduleAsync(workout, tomorrow);
        }
        else if (choice == optDayAfter)
        {
            await ExecuteRescheduleAsync(workout, dayAfterTomorrow);
        }
        else if (choice == optNextWeek)
        {
            await ExecuteRescheduleAsync(workout, nextWeek);
        }
        else if (choice == optCustom)
        {
            RequestRescheduleDateSelection?.Invoke(workout);
        }
    }

    public async Task ExecuteRescheduleAsync(CalendarWorkoutItem workout, DateTime newDate)
    {
        if (!workout.ProgramWorkoutId.HasValue || !workout.ScheduledDate.HasValue) return;

        var origDate = workout.ScheduledDate.Value.Date;
        var targetDate = newDate.Date;

        if (targetDate == origDate)
        {
            await Shell.Current.DisplayAlertAsync("Та же дата", "Тренировка уже запланирована на эту дату.", "OK");
            return;
        }

        if (targetDate < DateTime.Today)
        {
            var confirmPast = await Shell.Current.DisplayAlertAsync(
                "Прошедшая дата",
                $"Вы выбрали прошедшую дату ({targetDate:d MMMM yyyy}). Тренировка будет отмечена как пропущенная. Перенести?",
                "Да, перенести",
                "Отмена");
            if (!confirmPast) return;
        }

        var existingOnDate = await _calendarService.GetWorkoutsForDateAsync(targetDate);
        if (existingOnDate.Count > 0)
        {
            var confirmOccupied = await Shell.Current.DisplayAlertAsync(
                "Дата уже занята",
                $"На {targetDate:d MMMM yyyy} уже есть тренировка «{existingOnDate[0].Title}». Всё равно перенести?",
                "Перенести",
                "Отмена");
            if (!confirmOccupied) return;
        }

        await _scheduleRepo.RescheduleWorkoutAsync(workout.ProgramWorkoutId.Value, origDate, targetDate);
        SelectedDate = targetDate;
        CurrentPeriodDate = targetDate;
        InvalidateCalendarCache();
        await RefreshCalendarAsync();

        await Shell.Current.DisplayAlertAsync(
            "Тренировка перенесена",
            $"«{workout.Title}» перенесена на {targetDate:d MMMM yyyy}.",
            "OK");
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
