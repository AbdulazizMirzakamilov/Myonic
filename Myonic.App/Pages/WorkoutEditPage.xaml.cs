using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class WorkoutEditPage : ContentPage
{
    private readonly WorkoutEditViewModel _viewModel;

    public WorkoutEditPage(WorkoutEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadExercisesAsync();
    }
}
