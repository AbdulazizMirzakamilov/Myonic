using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class GoalEditPage : ContentPage
{
    public GoalEditPage(GoalEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
