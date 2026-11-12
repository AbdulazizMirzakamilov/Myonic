using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Core.Services;

#if ANDROID
using Myonic.Platforms.Android;
#elif WINDOWS
using Myonic.Platforms.Windows;
#endif

namespace Myonic.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationSettingsService _settingsService;
    private readonly INotificationScheduler _scheduler;
    private readonly IWorkoutScheduleRepository _scheduleRepository;
    private readonly IWorkoutSessionRepository _sessionRepository;
    private readonly IBodyMeasurementRepository _measurementRepository;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

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
        return true;
#elif WINDOWS
        try
        {
            return WindowsNotificationHelper.AreNotificationsEnabled();
        }
        catch
        {
            return true;
        }
#else
        return await Task.FromResult(true);
#endif
    }

    public async Task ShowNotificationAsync(int id, string title, string body, NotificationCategory category)
    {
        if (!IsNotificationEnabled(category))
        {
            return;
        }

#if ANDROID
        try
        {
            AndroidNotificationHelper.ShowNotification(Android.App.Application.Context, id, title, body, category);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Android ShowNotification error: {ex.Message}");
        }
#elif WINDOWS
        try
        {
            WindowsNotificationHelper.ShowNotification(id, title, body, category);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Windows ShowNotification error: {ex.Message}");
        }
#endif

        // Внутриприкладное уведомление для мгновенной обратной связи при открытом окне
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
        if (!IsNotificationEnabled(category))
        {
            return;
        }

        if (notifyTime <= DateTime.Now)
        {
            return;
        }

#if ANDROID
        try
        {
            AndroidNotificationHelper.ScheduleNotification(Android.App.Application.Context, id, title, body, notifyTime, category);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Android ScheduleNotification error: {ex.Message}");
        }
#elif WINDOWS
        try
        {
            WindowsNotificationHelper.ScheduleNotification(id, title, body, notifyTime, category);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Windows ScheduleNotification error: {ex.Message}");
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
#elif WINDOWS
        try
        {
            WindowsNotificationHelper.CancelNotification(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Windows CancelNotification error: {ex.Message}");
        }
#endif
        await Task.CompletedTask;
    }

    public async Task CancelAllAsync()
    {
#if WINDOWS
        try
        {
            WindowsNotificationHelper.CancelAll();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Windows CancelAll error: {ex.Message}");
        }
#endif
        await CancelNotificationAsync(NotificationIds.UpcomingWorkoutId);
        await CancelNotificationAsync(NotificationIds.MissedWorkoutId);
        await CancelNotificationAsync(NotificationIds.WeightReminderId);
        await CancelNotificationAsync(NotificationIds.TestNotificationId);
    }

    public async Task SyncScheduledRemindersAsync()
    {
        if (!await _syncLock.WaitAsync(0))
        {
            // Another sync is currently in progress; avoid redundant overlapping executions
            return;
        }

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

            // Отменяем ранее запланированные системные напоминания для исключения дублирования
            await CancelNotificationAsync(NotificationIds.UpcomingWorkoutId);
            await CancelNotificationAsync(NotificationIds.MissedWorkoutId);
            await CancelNotificationAsync(NotificationIds.WeightReminderId);

            // Регистрируем вычисленные уведомления
            foreach (var item in itemsToSchedule)
            {
                await ScheduleNotificationAsync(item.Id, item.Title, item.Body, item.ScheduledTime, item.Category);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SyncScheduledRemindersAsync error: {ex.Message}");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private bool IsNotificationEnabled(NotificationCategory category) => category switch
    {
        NotificationCategory.UpcomingWorkout => _settingsService.EnableUpcomingWorkoutReminders,
        NotificationCategory.MissedWorkout => _settingsService.EnableMissedWorkoutAlerts,
        NotificationCategory.WeightMeasurement => _settingsService.EnableWeightReminders,
        _ => true
    };
}
