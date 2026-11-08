namespace Myonic.Core.Services;

public interface INotificationSettingsService
{
    bool EnableUpcomingWorkoutReminders { get; set; }
    TimeSpan UpcomingWorkoutTime { get; set; }

    bool EnableMissedWorkoutAlerts { get; set; }
    TimeSpan MissedWorkoutTime { get; set; }

    bool EnableWeightReminders { get; set; }
    TimeSpan WeightReminderTime { get; set; }

    void SaveSettings();
    void ResetToDefaults();
}
