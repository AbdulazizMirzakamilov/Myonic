using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class ProgramEditPage : ContentPage
{
    private readonly ProgramEditViewModel _viewModel;

    public ProgramEditPage(ProgramEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Обновляем тренировки при возврате с экрана тренировки
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadWorkoutsCommand.ExecuteAsync(null);
    }
}