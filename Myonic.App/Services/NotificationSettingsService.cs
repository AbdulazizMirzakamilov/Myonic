using Microsoft.Maui.Storage;
using Myonic.Core.Services;

namespace Myonic.Services;

public class NotificationSettingsService : INotificationSettingsService
{
    private const string EnableUpcomingKey = "notif_enable_upcoming_workouts";
    private const string UpcomingTimeKey = "notif_upcoming_workout_time_minutes";

    private const string EnableMissedKey = "notif_enable_missed_workouts";
    private const string MissedTimeKey = "notif_missed_workout_time_minutes";

    private const string EnableWeightKey = "notif_enable_weight_reminders";
    private const string WeightTimeKey = "notif_weight_reminder_time_minutes";

    public bool EnableUpcomingWorkoutReminders
    {
        get => Preferences.Get(EnableUpcomingKey, true);
        set => Preferences.Set(EnableUpcomingKey, value);
    }

    public TimeSpan UpcomingWorkoutTime
    {
        get => TimeSpan.FromMinutes(Preferences.Get(UpcomingTimeKey, 9 * 60)); // 09:00 default
        set => Preferences.Set(UpcomingTimeKey, (int)value.TotalMinutes);
    }

    public bool EnableMissedWorkoutAlerts
    {
        get => Preferences.Get(EnableMissedKey, true);
        set => Preferences.Set(EnableMissedKey, value);
    }

    public TimeSpan MissedWorkoutTime
    {
        get => TimeSpan.FromMinutes(Preferences.Get(MissedTimeKey, 20 * 60 + 30)); // 20:30 default
        set => Preferences.Set(MissedTimeKey, (int)value.TotalMinutes);
    }

    public bool EnableWeightReminders
    {
        get => Preferences.Get(EnableWeightKey, true);
        set => Preferences.Set(EnableWeightKey, value);
    }

    public TimeSpan WeightReminderTime
    {
        get => TimeSpan.FromMinutes(Preferences.Get(WeightTimeKey, 8 * 60)); // 08:00 default
        set => Preferences.Set(WeightTimeKey, (int)value.TotalMinutes);
    }

    public void SaveSettings()
    {
        // Preferences.Set persists immediately in .NET MAUI
    }

    public void ResetToDefaults()
    {
        EnableUpcomingWorkoutReminders = true;
        UpcomingWorkoutTime = new TimeSpan(9, 0, 0);

        EnableMissedWorkoutAlerts = true;
        MissedWorkoutTime = new TimeSpan(20, 30, 0);

        EnableWeightReminders = true;
        WeightReminderTime = new TimeSpan(8, 0, 0);
    }
}
