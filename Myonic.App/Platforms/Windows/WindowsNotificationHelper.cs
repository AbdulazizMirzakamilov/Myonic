#if WINDOWS
using System.Security;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Myonic.Core.Models;

namespace Myonic.Platforms.Windows;

public static class WindowsNotificationHelper
{
    public const string AppId = "Myonic";
    public const string GroupName = "Myonic";

    public static bool AreNotificationsEnabled()
    {
        try
        {
            var notifier = ToastNotificationManager.CreateToastNotifier(AppId);
            return notifier?.Setting == NotificationSetting.Enabled;
        }
        catch
        {
            return true;
        }
    }

    public static void ShowNotification(int id, string title, string body, NotificationCategory category)
    {
        try
        {
            var notifier = ToastNotificationManager.CreateToastNotifier(AppId);
            if (notifier == null) return;

            var xml = BuildToastXml(title, body);
            var toast = new ToastNotification(xml)
            {
                Tag = GetTag(id),
                Group = GroupName,
                ExpirationTime = DateTimeOffset.Now.AddHours(4)
            };

            notifier.Show(toast);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowsNotificationHelper] ShowNotification error: {ex.Message}");
        }
    }

    public static void ScheduleNotification(int id, string title, string body, DateTime notifyTime, NotificationCategory category)
    {
        try
        {
            var localTime = notifyTime.ToLocalTime();
            var deliveryTime = new DateTimeOffset(localTime);

            // Нельзя запланировать уведомление в прошлом
            if (deliveryTime <= DateTimeOffset.Now.AddSeconds(2))
            {
                return;
            }

            var notifier = ToastNotificationManager.CreateToastNotifier(AppId);
            if (notifier == null) return;

            // Удаляем предыдущую запланированную запись с тем же Id для исключения дубликатов
            CancelNotification(id);

            var xml = BuildToastXml(title, body);
            var scheduledToast = new ScheduledToastNotification(xml, deliveryTime)
            {
                Id = id.ToString(),
                Tag = GetTag(id),
                Group = GroupName
            };

            notifier.AddToSchedule(scheduledToast);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowsNotificationHelper] ScheduleNotification error: {ex.Message}");
        }
    }

    public static void CancelNotification(int id)
    {
        try
        {
            var notifier = ToastNotificationManager.CreateToastNotifier(AppId);
            if (notifier == null) return;

            var targetTag = GetTag(id);
            var idString = id.ToString();
            var scheduledList = notifier.GetScheduledToastNotifications();

            if (scheduledList != null)
            {
                foreach (var item in scheduledList)
                {
                    if (item.Tag == targetTag || item.Id == idString)
                    {
                        notifier.RemoveFromSchedule(item);
                    }
                }
            }

            // Очищаем также из центра уведомлений Windows, если уже отобразилось
            try
            {
                ToastNotificationManager.History.Remove(targetTag, GroupName, AppId);
            }
            catch
            {
                // Игнорируем ошибку удаления из истории
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowsNotificationHelper] CancelNotification error: {ex.Message}");
        }
    }

    public static void CancelAll()
    {
        try
        {
            var notifier = ToastNotificationManager.CreateToastNotifier(AppId);
            if (notifier == null) return;

            var scheduledList = notifier.GetScheduledToastNotifications();
            if (scheduledList != null)
            {
                foreach (var item in scheduledList)
                {
                    if (item.Group == GroupName || (item.Tag != null && item.Tag.StartsWith("Myonic_")))
                    {
                        notifier.RemoveFromSchedule(item);
                    }
                }
            }

            try
            {
                ToastNotificationManager.History.Clear(AppId);
            }
            catch
            {
                // Игнорируем ошибку очистки истории
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowsNotificationHelper] CancelAll error: {ex.Message}");
        }
    }

    private static string GetTag(int id) => $"Myonic_{id}";

    private static XmlDocument BuildToastXml(string title, string body)
    {
        var escapedTitle = SecurityElement.Escape(title) ?? string.Empty;
        var escapedBody = SecurityElement.Escape(body) ?? string.Empty;

        var xmlPayload = $@"
<toast scenario=""reminder"">
    <visual>
        <binding template=""ToastGeneric"">
            <text>{escapedTitle}</text>
            <text>{escapedBody}</text>
        </binding>
    </visual>
    <audio src=""ms-winsoundevent:Notification.Default"" />
</toast>";

        var doc = new XmlDocument();
        doc.LoadXml(xmlPayload);
        return doc;
    }
}
#endif
