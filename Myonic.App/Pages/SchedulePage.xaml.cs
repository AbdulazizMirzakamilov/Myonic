using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class SchedulePage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;
    private bool _isDragging;
    private bool _isAnimating;
    private bool _isGestureHorizontal;
    private const double HorizontalThreshold = 14.0;
    private const double SwipeTriggerDistance = 60.0;

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

    private async void OnCalendarPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_isAnimating) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isDragging = false;
                _isGestureHorizontal = false;
                break;

            case GestureStatus.Running:
                if (!_isGestureHorizontal)
                {
                    // Check if movement is clearly horizontal
                    if (Math.Abs(e.TotalX) > HorizontalThreshold && Math.Abs(e.TotalX) > Math.Abs(e.TotalY) * 1.4)
                    {
                        _isGestureHorizontal = true;
                        _isDragging = true;
                    }
                    else if (Math.Abs(e.TotalY) > 20 && Math.Abs(e.TotalY) > Math.Abs(e.TotalX))
                    {
                        // Vertical scroll of parent ScrollView, do not intercept
                        _isDragging = false;
                        return;
                    }
                }

                if (_isDragging && CalendarViewHost != null)
                {
                    // Drag calendar container in real-time under the finger
                    CalendarViewHost.TranslationX = e.TotalX;
                    var absX = Math.Abs(e.TotalX);
                    CalendarViewHost.Opacity = Math.Max(0.65, 1.0 - (absX / 600.0));
                }
                break;

            case GestureStatus.Completed:
                if (_isAnimating) return;
                if (!_isDragging || CalendarViewHost == null)
                {
                    _isDragging = false;
                    _isGestureHorizontal = false;
                    return;
                }

                _isAnimating = true;
                _isDragging = false;

                double totalX = e.TotalX;
                double width = CalendarOuterCard?.Width > 0 ? CalendarOuterCard.Width : 360;

                if (totalX < -SwipeTriggerDistance)
                {
                    // Dragged Left -> Next period
                    await CalendarViewHost.TranslateToAsync(-width, 0, 140, Easing.CubicOut);

                    await _viewModel.NextPeriodAsync();

                    CalendarViewHost.TranslationX = width;
                    CalendarViewHost.Opacity = 0.7;

                    await Task.WhenAll(
                        CalendarViewHost.TranslateToAsync(0, 0, 180, Easing.CubicOut),
                        CalendarViewHost.FadeToAsync(1.0, 180, Easing.CubicOut)
                    );
                }
                else if (totalX > SwipeTriggerDistance)
                {
                    // Dragged Right -> Previous period
                    await CalendarViewHost.TranslateToAsync(width, 0, 140, Easing.CubicOut);

                    await _viewModel.PreviousPeriodAsync();

                    CalendarViewHost.TranslationX = -width;
                    CalendarViewHost.Opacity = 0.7;

                    await Task.WhenAll(
                        CalendarViewHost.TranslateToAsync(0, 0, 180, Easing.CubicOut),
                        CalendarViewHost.FadeToAsync(1.0, 180, Easing.CubicOut)
                    );
                }
                else
                {
                    // Drag distance not enough -> Snap smoothly back
                    await Task.WhenAll(
                        CalendarViewHost.TranslateToAsync(0, 0, 180, Easing.SpringOut),
                        CalendarViewHost.FadeToAsync(1.0, 180, Easing.CubicOut)
                    );
                }

                if (CalendarViewHost != null)
                {
                    CalendarViewHost.TranslationX = 0;
                    CalendarViewHost.Opacity = 1.0;
                }
                _isAnimating = false;
                _isGestureHorizontal = false;
                break;

            case GestureStatus.Canceled:
                if (_isDragging && CalendarViewHost != null)
                {
                    _isAnimating = true;
                    await Task.WhenAll(
                        CalendarViewHost.TranslateToAsync(0, 0, 180, Easing.CubicOut),
                        CalendarViewHost.FadeToAsync(1.0, 180, Easing.CubicOut)
                    );
                    CalendarViewHost.TranslationX = 0;
                    CalendarViewHost.Opacity = 1.0;
                }
                _isDragging = false;
                _isAnimating = false;
                _isGestureHorizontal = false;
                break;
        }
    }
}
