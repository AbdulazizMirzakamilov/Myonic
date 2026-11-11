using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class GoalsPage : ContentPage
{
    private readonly GoalsViewModel _vm;

    public GoalsPage(GoalsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.ActiveWorkoutTracker != null)
        {
            await _vm.ActiveWorkoutTracker.RefreshAsync();
        }
        await _vm.LoadAsync();
    }
}
