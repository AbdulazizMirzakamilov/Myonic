using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class OneRepMaxPage : ContentPage
{
    public OneRepMaxPage(OneRepMaxViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
