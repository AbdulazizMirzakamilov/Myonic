#if ANDROID
#pragma warning disable CA1416

using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Myonic.Core.Models;

namespace Myonic.Platforms.Android;

public static class AndroidNotificationHelper
{
    public const string ChannelWorkouts = "channel_workouts";
    public const string ChannelMissed = "channel_missed";
    public const string ChannelMeasurements = "channel_measurements";
    public const string ChannelGeneral = "channel_general";

    public static bool IsNotificationCategoryEnabled(NotificationCategory category)
    {
        return category switch
        {
            NotificationCategory.UpcomingWorkout => Microsoft.Maui.Storage.Preferences.Get("notif_enable_upcoming_workouts", true),
            NotificationCategory.MissedWorkout => Microsoft.Maui.Storage.Preferences.Get("notif_enable_missed_workouts", true),
            NotificationCategory.WeightMeasurement => Microsoft.Maui.Storage.Preferences.Get("notif_enable_weight_reminders", true),
            _ => true
        };
    }

    public static void EnsureChannels(Context context)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var notificationManager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            if (notificationManager == null) return;

            var channels = new List<NotificationChannel>
            {
                new NotificationChannel(ChannelWorkouts, "Напоминания о тренировках", NotificationImportance.High)
                {
                    Description = "Оповещения о запланированных тренировках на сегодня"
                },
                new NotificationChannel(ChannelMissed, "Пропущенные тренировки", NotificationImportance.High)
                {
                    Description = "Вечерние напоминания о пропущенных запланированных тренировках"
                },
                new NotificationChannel(ChannelMeasurements, "Замеры тела и вес", NotificationImportance.Default)
                {
                    Description = "Утренние напоминания о фиксации веса и параметров тела"
                },
                new NotificationChannel(ChannelGeneral, "Общие оповещения Myonic", NotificationImportance.Default)
                {
                    Description = "Тестовые и системные уведомления приложения"
                }
            };

            notificationManager.CreateNotificationChannels(channels);
        }
    }

    public static void ShowNotification(Context context, int id, string title, string body, NotificationCategory category)
    {
        if (!IsNotificationCategoryEnabled(category))
        {
            return;
        }

        EnsureChannels(context);

        string channelId = category switch
        {
            NotificationCategory.UpcomingWorkout => ChannelWorkouts,
            NotificationCategory.MissedWorkout => ChannelMissed,
            NotificationCategory.WeightMeasurement => ChannelMeasurements,
            _ => ChannelGeneral
        };

        var launchIntent = new Intent(context, typeof(MainActivity));
        launchIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }

        var pendingIntent = PendingIntent.GetActivity(context, id, launchIntent, pendingIntentFlags);

        int icon = context.ApplicationInfo?.Icon ?? global::Android.Resource.Drawable.IcDialogInfo;
        if (icon == 0)
        {
            icon = global::Android.Resource.Drawable.IcDialogInfo;
        }

        var builder = new NotificationCompat.Builder(context, channelId);
        builder.SetSmallIcon(icon);
        builder.SetContentTitle(title);
        builder.SetContentText(body);

        var bigTextStyle = new NotificationCompat.BigTextStyle();
        bigTextStyle.BigText(body);
        builder.SetStyle(bigTextStyle);
        builder.SetPriority(NotificationCompat.PriorityHigh);
        builder.SetAutoCancel(true);

        if (pendingIntent != null)
        {
            builder.SetContentIntent(pendingIntent);
        }

        var manager = NotificationManagerCompat.From(context);
        if (manager != null)
        {
            using var builtNotification = builder.Build();
            if (builtNotification != null)
            {
                manager.Notify(id, builtNotification);
            }
        }
    }

    public static void ScheduleNotification(Context context, int id, string title, string body, DateTime notifyTime, NotificationCategory category)
    {
        if (!IsNotificationCategoryEnabled(category))
        {
            return;
        }

        EnsureChannels(context);

        var utcTime = notifyTime.ToUniversalTime();
        var triggerMillis = (long)(utcTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        var nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Не планируем устаревшие уведомления в прошлом
        if (triggerMillis <= nowMillis + 2000)
        {
            return;
        }

        var intent = new Intent(context, typeof(NotificationAlarmReceiver));
        intent.PutExtra("id", id);
        intent.PutExtra("title", title);
        intent.PutExtra("body", body);
        intent.PutExtra("category", (int)category);

        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }

        var pendingIntent = PendingIntent.GetBroadcast(context, id, intent, pendingIntentFlags);
        if (pendingIntent == null) return;

        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager == null) return;

        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S && alarmManager.CanScheduleExactAlarms())
                {
                    alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerMillis, pendingIntent);
                }
                else
                {
                    alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerMillis, pendingIntent);
                }
            }
            else
            {
                alarmManager.Set(AlarmType.RtcWakeup, triggerMillis, pendingIntent);
            }
        }
        catch
        {
            try
            {
                alarmManager.Set(AlarmType.RtcWakeup, triggerMillis, pendingIntent);
            }
            catch
            {
                // Silently swallow if OS restricts alarm scheduling
            }
        }
    }

    public static void CancelNotification(Context context, int id)
    {
        var intent = new Intent(context, typeof(NotificationAlarmReceiver));
        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }

        var pendingIntent = PendingIntent.GetBroadcast(context, id, intent, pendingIntentFlags);
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (pendingIntent != null)
        {
            alarmManager?.Cancel(pendingIntent);
        }

        var manager = NotificationManagerCompat.From(context);
        manager?.Cancel(id);
    }
}

#pragma warning restore CA1416
#endif
