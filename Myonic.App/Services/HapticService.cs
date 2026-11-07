using Microsoft.Maui.Devices;

namespace Myonic.Services;

public class HapticService : IHapticService
{
    public void TriggerClick()
    {
        if (!Preferences.Get("sound_vibration_enabled", true)) return;

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Fallback for platforms or devices without haptics
        }
    }

    public void TriggerSuccess()
    {
        if (!Preferences.Get("sound_vibration_enabled", true)) return;

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
        catch
        {
            // Fallback for platforms or devices without haptics
        }
    }
}
