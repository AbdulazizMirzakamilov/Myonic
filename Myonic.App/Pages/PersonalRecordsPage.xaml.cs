using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class PersonalRecordsPage : ContentPage
{
    private readonly PersonalRecordsViewModel _viewModel;

    public PersonalRecordsPage(PersonalRecordsViewModel viewModel)
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
