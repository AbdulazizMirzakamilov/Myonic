using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class BodyMeasurementsPage : ContentPage
{
    private readonly BodyMeasurementsViewModel _vm;

    public BodyMeasurementsPage(BodyMeasurementsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
        _vm.RequestChartRedraw += () =>
        {
            MainThread.BeginInvokeOnMainThread(() => ChartView?.Invalidate());
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        ChartView?.Invalidate();
    }
}
