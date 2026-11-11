using Myonic.Core.Calendar;
using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class SchedulePage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;
    private CalendarWorkoutItem? _reschedulingWorkout;

    public SchedulePage(ScheduleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _viewModel.RequestRescheduleDateSelection += workout =>
        {
            _reschedulingWorkout = workout;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (HiddenDatePicker != null)
                {
                    HiddenDatePicker.Date = workout.ScheduledDate ?? DateTime.Today;
                    HiddenDatePicker.Focus();
                }
            });
        };
    }

    private async void OnRescheduleDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (_reschedulingWorkout != null && e.NewDate.HasValue)
        {
            var workout = _reschedulingWorkout;
            _reschedulingWorkout = null;
            await _viewModel.ExecuteRescheduleAsync(workout, e.NewDate.Value);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.NotifyAllThemeElements();
        await _viewModel.RefreshCalendarAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Do not unsubscribe singleton ViewModel from theme changes,
        // so it seamlessly reflects themes when user toggles themes in Settings
    }

    private async void OnDayTapped(object? sender, TappedEventArgs e)
    {
        if (sender is VisualElement { BindingContext: CalendarDayItem dayItem })
        {
            await _viewModel.SelectDayAsync(dayItem);
        }
    }

    private async void OnWorkoutDetailClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement { BindingContext: CalendarWorkoutItem workout })
        {
            await _viewModel.OpenWorkoutDetailAsync(workout);
        }
    }

    private async void OnStartWorkoutClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement { BindingContext: CalendarWorkoutItem workout })
        {
            await _viewModel.StartPlannedWorkoutAsync(workout);
        }
    }

    // NOTE: Swipe gestures were removed to ensure maximum performance, responsiveness,
    // and seamless vertical scrolling across mobile devices and desktop.
    // If swipe-based navigation is desired in the future, gestures can be re-attached
    // to CalendarOuterCard calling _viewModel.NextPeriodAsync() / PreviousPeriodAsync().
}
