// Pages/ExerciseEditPage.xaml.cs
using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class ExerciseEditPage : ContentPage
{
    public ExerciseEditPage(ExerciseEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}