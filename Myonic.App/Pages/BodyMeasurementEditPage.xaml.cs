using Myonic.ViewModels;

namespace Myonic.Pages;

public partial class BodyMeasurementEditPage : ContentPage
{
    public BodyMeasurementEditPage(BodyMeasurementEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
