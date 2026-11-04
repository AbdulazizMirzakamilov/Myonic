using Myonic.Core.Calendar;
using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class SchedulePage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;
    private bool _isAnimating;

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

    private async void OnCalendarSwipedLeft(object? sender, SwipedEventArgs e)
    {
        await AnimatePeriodTransitionAsync(forward: true);
    }

    private async void OnCalendarSwipedRight(object? sender, SwipedEventArgs e)
    {
        await AnimatePeriodTransitionAsync(forward: false);
    }

    private async void OnPreviousPeriodClicked(object? sender, EventArgs e)
    {
        await AnimatePeriodTransitionAsync(forward: false);
    }

    private async void OnNextPeriodClicked(object? sender, EventArgs e)
    {
        await AnimatePeriodTransitionAsync(forward: true);
    }

    private async void OnTodayClicked(object? sender, EventArgs e)
    {
        await _viewModel.TodayAsync();
    }

    private async void OnSwitchToWeekViewTapped(object? sender, TappedEventArgs e)
    {
        await _viewModel.SwitchToWeekViewAsync();
    }

    private async void OnSwitchToMonthViewTapped(object? sender, TappedEventArgs e)
    {
        await _viewModel.SwitchToMonthViewAsync();
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

    private async Task AnimatePeriodTransitionAsync(bool forward)
    {
        if (_isAnimating || CalendarViewHost == null) return;
        _isAnimating = true;

        try
        {
            double width = CalendarOuterCard?.Width > 0 ? CalendarOuterCard.Width : 350;
            double outX = forward ? -width : width;
            double inX = forward ? width : -width;

            // 1. Slide out current period
            await Task.WhenAll(
                CalendarViewHost.TranslateToAsync(outX, 0, 110, Easing.CubicIn),
                CalendarViewHost.FadeToAsync(0.3, 110, Easing.CubicIn)
            );

            // 2. Change period data in ViewModel
            if (forward)
            {
                await _viewModel.NextPeriodAsync();
            }
            else
            {
                await _viewModel.PreviousPeriodAsync();
            }

            // 3. Position for slide-in
            CalendarViewHost.TranslationX = inX;

            // 4. Slide in new period
            await Task.WhenAll(
                CalendarViewHost.TranslateToAsync(0, 0, 130, Easing.CubicOut),
                CalendarViewHost.FadeToAsync(1.0, 130, Easing.CubicOut)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CalendarAnimation] Transition error: {ex.Message}");
        }
        finally
        {
            if (CalendarViewHost != null)
            {
                CalendarViewHost.TranslationX = 0;
                CalendarViewHost.Opacity = 1.0;
            }
            _isAnimating = false;
        }
    }
}
