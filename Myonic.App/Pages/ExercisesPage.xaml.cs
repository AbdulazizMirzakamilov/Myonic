using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class ExercisesPage : ContentPage
{
    private readonly ExercisesViewModel _viewModel;

    public ExercisesPage(ExercisesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Обновляем список при каждом возврате на экран (после добавления/правки)
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}