using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class WorkoutEditPage : ContentPage
{
    public WorkoutEditPage(WorkoutEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}