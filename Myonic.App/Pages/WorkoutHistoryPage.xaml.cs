using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class WorkoutHistoryPage : ContentPage
{
    private readonly WorkoutHistoryViewModel _vm;

    public WorkoutHistoryPage(WorkoutHistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        _vm.RequestChartsRedraw += () =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                VolumeChartView?.Invalidate();
                FrequencyChartView?.Invalidate();
            });
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadHistoryAsync();
        VolumeChartView?.Invalidate();
        FrequencyChartView?.Invalidate();
    }
}
