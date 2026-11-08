using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Core.Services;
using Xunit;

namespace Myonic.Tests;

public class TestNotificationSettings : INotificationSettingsService
{
    public bool EnableUpcomingWorkoutReminders { get; set; } = true;
    public TimeSpan UpcomingWorkoutTime { get; set; } = new(9, 0, 0);

    public bool EnableMissedWorkoutAlerts { get; set; } = true;
    public TimeSpan MissedWorkoutTime { get; set; } = new(20, 30, 0);

    public bool EnableWeightReminders { get; set; } = true;
    public TimeSpan WeightReminderTime { get; set; } = new(8, 0, 0);

    public void SaveSettings() { }
    public void ResetToDefaults() { }
}

public class NotificationSchedulerTests
{
    private readonly NotificationScheduler _scheduler = new();
    private readonly TestNotificationSettings _settings = new();

    [Fact]
    public void Calculate_WhenUpcomingWorkoutScheduledAndNotFinished_SchedulesForToday()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 7, 0, 0);
        var todayPlanned = new List<PlannedWorkoutSummary>
        {
            new() { WorkoutName = "Грудь + Трицепс", ProgramName = "Гипертрофия" }
        };
        var tomorrowPlanned = new List<PlannedWorkoutSummary>();
        var todaySessions = new List<WorkoutSessionDetail>();
        BodyMeasurement? todayMeasurement = null;

        // Act
        var result = _scheduler.CalculateScheduledNotifications(
            now, todayPlanned, tomorrowPlanned, todaySessions, todayMeasurement, _settings);

        // Assert
        var upcoming = result.FirstOrDefault(n => n.Id == NotificationIds.UpcomingWorkoutId);
        Assert.NotNull(upcoming);
        Assert.Equal(NotificationCategory.UpcomingWorkout, upcoming.Category);
        Assert.Equal(new DateTime(2026, 11, 9, 9, 0, 0), upcoming.ScheduledTime);
        Assert.Contains("Грудь + Трицепс", upcoming.Body);
    }

    [Fact]
    public void Calculate_WhenWorkoutAlreadyFinishedToday_DoesNotScheduleUpcomingForToday()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 7, 0, 0);
        var todayPlanned = new List<PlannedWorkoutSummary>
        {
            new() { WorkoutName = "Грудь" }
        };
        var tomorrowPlanned = new List<PlannedWorkoutSummary>();
        var todaySessions = new List<WorkoutSessionDetail>
        {
            new() { Id = 1, FinishedAt = new DateTime(2026, 11, 9, 6, 30, 0) }
        };

        // Act
        var result = _scheduler.CalculateScheduledNotifications(
            now, todayPlanned, tomorrowPlanned, todaySessions, null, _settings);

        // Assert
        var upcoming = result.FirstOrDefault(n => n.Id == NotificationIds.UpcomingWorkoutId);
        Assert.Null(upcoming);
    }

    [Fact]
    public void Calculate_WhenWorkoutFinishedToday_AndTomorrowHasWorkout_SchedulesTomorrow()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 7, 0, 0);
        var todayPlanned = new List<PlannedWorkoutSummary>
        {
            new() { WorkoutName = "Грудь" }
        };
        var tomorrowPlanned = new List<PlannedWorkoutSummary>
        {
            new() { WorkoutName = "Спина", ProgramName = "Сплит" }
        };
        var todaySessions = new List<WorkoutSessionDetail>
        {
            new() { Id = 1, FinishedAt = new DateTime(2026, 11, 9, 6, 30, 0) }
        };

        // Act
        var result = _scheduler.CalculateScheduledNotifications(
            now, todayPlanned, tomorrowPlanned, todaySessions, null, _settings);

        // Assert
        var upcoming = result.FirstOrDefault(n => n.Id == NotificationIds.UpcomingWorkoutId);
        Assert.NotNull(upcoming);
        Assert.Equal(new DateTime(2026, 11, 10, 9, 0, 0), upcoming.ScheduledTime);
        Assert.Contains("Спина", upcoming.Body);
    }

    [Fact]
    public void Calculate_WhenMissedWorkoutAlertEnabledAndWorkoutNotCompleted_SchedulesMissedAlert()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 14, 0, 0);
        var todayPlanned = new List<PlannedWorkoutSummary>
        {
            new() { WorkoutName = "Ноги" }
        };
        var todaySessions = new List<WorkoutSessionDetail>();

        // Act
        var result = _scheduler.CalculateScheduledNotifications(
            now, todayPlanned, [], todaySessions, null, _settings);

        // Assert
        var missed = result.FirstOrDefault(n => n.Id == NotificationIds.MissedWorkoutId);
        Assert.NotNull(missed);
        Assert.Equal(NotificationCategory.MissedWorkout, missed.Category);
        Assert.Equal(new DateTime(2026, 11, 9, 20, 30, 0), missed.ScheduledTime);
        Assert.Contains("Ноги", missed.Body);
    }

    [Fact]
    public void Calculate_WhenWorkoutCompletedToday_DoesNotScheduleMissedAlert()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 14, 0, 0);
        var todayPlanned = new List<PlannedWorkoutSummary>
        {
            new() { WorkoutName = "Ноги" }
        };
        var todaySessions = new List<WorkoutSessionDetail>
        {
            new() { Id = 1, FinishedAt = new DateTime(2026, 11, 9, 11, 0, 0) }
        };

        // Act
        var result = _scheduler.CalculateScheduledNotifications(
            now, todayPlanned, [], todaySessions, null, _settings);

        // Assert
        var missed = result.FirstOrDefault(n => n.Id == NotificationIds.MissedWorkoutId);
        Assert.Null(missed);
    }

    [Fact]
    public void Calculate_WhenNoWeightLoggedToday_SchedulesWeightReminderForToday()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 6, 0, 0);

        // Act
        var result = _scheduler.CalculateScheduledNotifications(
            now, [], [], [], null, _settings);

        // Assert
        var weight = result.FirstOrDefault(n => n.Id == NotificationIds.WeightReminderId);
        Assert.NotNull(weight);
        Assert.Equal(NotificationCategory.WeightMeasurement, weight.Category);
        Assert.Equal(new DateTime(2026, 11, 9, 8, 0, 0), weight.ScheduledTime);
    }

    [Fact]
    public void Calculate_WhenWeightAlreadyLoggedToday_SchedulesWeightReminderForTomorrow()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 6, 0, 0);
        var todayMeasurement = new BodyMeasurement
        {
            Date = new DateTime(2026, 11, 9),
            WeightKg = 81.2
        };

        // Act
        var result = _scheduler.CalculateScheduledNotifications(
            now, [], [], [], todayMeasurement, _settings);

        // Assert
        var weight = result.FirstOrDefault(n => n.Id == NotificationIds.WeightReminderId);
        Assert.NotNull(weight);
        Assert.Equal(new DateTime(2026, 11, 10, 8, 0, 0), weight.ScheduledTime);
    }

    [Fact]
    public void Calculate_WhenAllRemindersDisabled_ReturnsEmptyList()
    {
        // Arrange
        var now = new DateTime(2026, 11, 9, 6, 0, 0);
        var disabledSettings = new TestNotificationSettings
        {
            EnableUpcomingWorkoutReminders = false,
            EnableMissedWorkoutAlerts = false,
            EnableWeightReminders = false
        };
        var todayPlanned = new List<PlannedWorkoutSummary>
        {
            new() { WorkoutName = "Full Body" }
        };

        // Act
        var result = _scheduler.CalculateScheduledNotifications(
            now, todayPlanned, [], [], null, disabledSettings);

        // Assert
        Assert.Empty(result);
    }
}
