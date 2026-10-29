using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class WorkoutSessionDetailPage : ContentPage
{
    private readonly WorkoutSessionDetailViewModel _viewModel;

    public WorkoutSessionDetailPage(WorkoutSessionDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
