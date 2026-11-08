using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Core.Services;

#if ANDROID
using Myonic.Platforms.Android;
#endif

namespace Myonic.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationSettingsService _settingsService;
    private readonly INotificationScheduler _scheduler;
    private readonly IWorkoutScheduleRepository _scheduleRepository;
    private readonly IWorkoutSessionRepository _sessionRepository;
    private readonly IBodyMeasurementRepository _measurementRepository;

    public NotificationService(
        INotificationSettingsService settingsService,
        INotificationScheduler scheduler,
        IWorkoutScheduleRepository scheduleRepository,
        IWorkoutSessionRepository sessionRepository,
        IBodyMeasurementRepository measurementRepository)
    {
        _settingsService = settingsService;
        _scheduler = scheduler;
        _scheduleRepository = scheduleRepository;
        _sessionRepository = sessionRepository;
        _measurementRepository = measurementRepository;
    }

    public async Task<bool> RequestPermissionsAsync()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
            return status == PermissionStatus.Granted;
        }
#endif
        return true;
    }

    public async Task ShowNotificationAsync(int id, string title, string body, NotificationCategory category)
    {
#if ANDROID
        try
        {
            AndroidNotificationHelper.ShowNotification(Android.App.Application.Context, id, title, body, category);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Android ShowNotification error: {ex.Message}");
        }
#endif

        // In-app visual toast feedback across platforms
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var toast = Toast.Make($"{title}\n{body}", ToastDuration.Long, 14);
                await toast.Show();
            });
        }
        catch
        {
            // Ignore toast UI errors if window not focused
        }
    }

    public async Task ScheduleNotificationAsync(int id, string title, string body, DateTime notifyTime, NotificationCategory category)
    {
#if ANDROID
        try
        {
            AndroidNotificationHelper.ScheduleNotification(Android.App.Application.Context, id, title, body, notifyTime, category);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Android ScheduleNotification error: {ex.Message}");
        }
#endif
        await Task.CompletedTask;
    }

    public async Task CancelNotificationAsync(int id)
    {
#if ANDROID
        try
        {
            AndroidNotificationHelper.CancelNotification(Android.App.Application.Context, id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Android CancelNotification error: {ex.Message}");
        }
#endif
        await Task.CompletedTask;
    }

    public async Task CancelAllAsync()
    {
        await CancelNotificationAsync(NotificationIds.UpcomingWorkoutId);
        await CancelNotificationAsync(NotificationIds.MissedWorkoutId);
        await CancelNotificationAsync(NotificationIds.WeightReminderId);
        await CancelNotificationAsync(NotificationIds.TestNotificationId);
    }

    public async Task SyncScheduledRemindersAsync()
    {
        try
        {
            var now = DateTime.Now;
            var todayPlanned = await _scheduleRepository.GetPlannedWorkoutsForDateAsync(now.Date);
            var tomorrowPlanned = await _scheduleRepository.GetPlannedWorkoutsForDateAsync(now.Date.AddDays(1));
            var todaySessions = await _sessionRepository.GetSessionsForDateAsync(now.Date);
            var todayMeasurement = await _measurementRepository.GetByDateAsync(now.Date);

            var itemsToSchedule = _scheduler.CalculateScheduledNotifications(
                now,
                todayPlanned,
                tomorrowPlanned,
                todaySessions,
                todayMeasurement,
                _settingsService);

            // Cancel any previously armed alarms for these recurring IDs
            await CancelNotificationAsync(NotificationIds.UpcomingWorkoutId);
            await CancelNotificationAsync(NotificationIds.MissedWorkoutId);
            await CancelNotificationAsync(NotificationIds.WeightReminderId);

            // Re-arm alarms for calculated schedule
            foreach (var item in itemsToSchedule)
            {
                await ScheduleNotificationAsync(item.Id, item.Title, item.Body, item.ScheduledTime, item.Category);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SyncScheduledRemindersAsync error: {ex.Message}");
        }
    }
}
