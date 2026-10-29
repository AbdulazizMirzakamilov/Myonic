using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class ActiveWorkoutPage : ContentPage
{
    private readonly ActiveWorkoutViewModel _vm;

    public ActiveWorkoutPage(ActiveWorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.StopTimer();
    }
}
