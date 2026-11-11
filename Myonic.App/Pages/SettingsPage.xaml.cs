using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.ActiveWorkoutTracker != null)
        {
            await _viewModel.ActiveWorkoutTracker.RefreshAsync();
        }
        await _viewModel.LoadBackupsListAsync();
    }
}
