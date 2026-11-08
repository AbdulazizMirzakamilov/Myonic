namespace Myonic.Core.Models;

public enum NotificationCategory
{
    UpcomingWorkout = 1,
    MissedWorkout = 2,
    WeightMeasurement = 3,
    General = 4
}

public static class NotificationIds
{
    public const int UpcomingWorkoutId = 2001;
    public const int MissedWorkoutId = 2002;
    public const int WeightReminderId = 2003;
    public const int TestNotificationId = 2099;
}

public class NotificationItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationCategory Category { get; set; }
    public DateTime ScheduledTime { get; set; }

    public NotificationItem() { }

    public NotificationItem(int id, string title, string body, NotificationCategory category, DateTime scheduledTime)
    {
        Id = id;
        Title = title;
        Body = body;
        Category = category;
        ScheduledTime = scheduledTime;
    }
}
