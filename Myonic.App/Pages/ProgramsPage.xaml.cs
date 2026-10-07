using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class ProgramsPage : ContentPage
{
    private readonly ProgramsViewModel _viewModel;

    public ProgramsPage(ProgramsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}