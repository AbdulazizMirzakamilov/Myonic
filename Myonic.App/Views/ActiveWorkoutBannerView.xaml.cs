using Microsoft.Maui.Controls;
using Myonic.Services;

namespace Myonic.Views;

public partial class ActiveWorkoutBannerView : ContentView
{
    public static readonly BindableProperty TrackerProperty =
        BindableProperty.Create(
            nameof(Tracker),
            typeof(IActiveWorkoutTracker),
            typeof(ActiveWorkoutBannerView),
            default(IActiveWorkoutTracker),
            propertyChanged: OnTrackerChanged);

    public IActiveWorkoutTracker? Tracker
    {
        get => (IActiveWorkoutTracker?)GetValue(TrackerProperty);
        set => SetValue(TrackerProperty, value);
    }

    public ActiveWorkoutBannerView()
    {
        InitializeComponent();
    }

    private static void OnTrackerChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        // Property change notification handled by binding
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (Tracker == null)
        {
            if (BindingContext is IHasActiveWorkoutTracker hasTracker)
            {
                Tracker = hasTracker.ActiveWorkoutTracker;
            }
            else if (BindingContext is IActiveWorkoutTracker directTracker)
            {
                Tracker = directTracker;
            }
        }
    }
}
