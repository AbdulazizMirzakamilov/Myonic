using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class ProgramExerciseEditPage : ContentPage
{
    public ProgramExerciseEditPage(ProgramExerciseEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
