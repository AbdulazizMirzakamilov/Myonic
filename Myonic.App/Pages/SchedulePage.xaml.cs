using Myonic.Core.Calendar;
using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class SchedulePage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;

    public SchedulePage(ScheduleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.SubscribeThemeEvents();
        await _viewModel.RefreshCalendarAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.UnsubscribeThemeEvents();
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
