using Myonic.Core.Models;

namespace Myonic.Core.Services;

public interface INotificationService
{
    Task<bool> RequestPermissionsAsync();
    Task ShowNotificationAsync(int id, string title, string body, NotificationCategory category);
    Task ScheduleNotificationAsync(int id, string title, string body, DateTime notifyTime, NotificationCategory category);
    Task CancelNotificationAsync(int id);
    Task CancelAllAsync();
    Task SyncScheduledRemindersAsync();
}
