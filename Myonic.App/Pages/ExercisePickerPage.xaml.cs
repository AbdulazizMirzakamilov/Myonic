using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class ExercisePickerPage : ContentPage
{
    private readonly ExercisePickerViewModel _viewModel;

    public ExercisePickerPage(ExercisePickerViewModel viewModel)
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