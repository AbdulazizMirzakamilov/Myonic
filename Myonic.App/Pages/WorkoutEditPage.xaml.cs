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

    // Обновляем список упражнений при возврате с экрана параметров
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadExercisesCommand.ExecuteAsync(null);
    }
}