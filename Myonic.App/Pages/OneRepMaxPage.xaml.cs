using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class OneRepMaxPage : ContentPage
{
    public OneRepMaxPage(OneRepMaxViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is OneRepMaxViewModel vm && vm.ActiveWorkoutTracker != null)
        {
            await vm.ActiveWorkoutTracker.RefreshAsync();
        }
    }
}
