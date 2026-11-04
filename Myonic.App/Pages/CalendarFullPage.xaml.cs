using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class CalendarFullPage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;

    private double _panX;
    private double _panY;
    private bool _panHandled;

    public CalendarFullPage(ScheduleViewModel viewModel)
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

    private void OnCalendarPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                _panX = e.TotalX;
                _panY = e.TotalY;
                // Only trigger if horizontal swipe is clearly dominant over vertical movement
                if (!_panHandled && Math.Abs(_panX) > 40 && Math.Abs(_panX) > 1.8 * Math.Abs(_panY))
                {
                    _panHandled = true;
                    if (_panX < 0)
                    {
                        _viewModel.NextPeriodCommand.Execute(null);
                    }
                    else
                    {
                        _viewModel.PreviousPeriodCommand.Execute(null);
                    }
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _panX = 0;
                _panY = 0;
                _panHandled = false;
                break;
        }
    }
}
