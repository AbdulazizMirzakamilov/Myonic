using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Core.Services;

public interface INotificationScheduler
{
    IReadOnlyList<NotificationItem> CalculateScheduledNotifications(
        DateTime now,
        IReadOnlyList<PlannedWorkoutSummary> todayPlannedWorkouts,
        IReadOnlyList<PlannedWorkoutSummary> tomorrowPlannedWorkouts,
        IReadOnlyList<WorkoutSessionDetail> todaySessions,
        BodyMeasurement? todayMeasurement,
        INotificationSettingsService settings);
}

public class NotificationScheduler : INotificationScheduler
{
    public IReadOnlyList<NotificationItem> CalculateScheduledNotifications(
        DateTime now,
        IReadOnlyList<PlannedWorkoutSummary> todayPlannedWorkouts,
        IReadOnlyList<PlannedWorkoutSummary> tomorrowPlannedWorkouts,
        IReadOnlyList<WorkoutSessionDetail> todaySessions,
        BodyMeasurement? todayMeasurement,
        INotificationSettingsService settings)
    {
        var items = new List<NotificationItem>();
        bool hasFinishedWorkoutToday = todaySessions.Any(s => s.FinishedAt != null);

        // 1. Upcoming Workout Reminder
        if (settings.EnableUpcomingWorkoutReminders)
        {
            DateTime todayWorkoutTime = now.Date + settings.UpcomingWorkoutTime;
            if (todayPlannedWorkouts.Count > 0 && !hasFinishedWorkoutToday && now <= todayWorkoutTime)
            {
                var workout = todayPlannedWorkouts[0];
                items.Add(new NotificationItem(
                    NotificationIds.UpcomingWorkoutId,
                    "🏋️ Запланирована тренировка!",
                    $"Сегодня по плану: {workout.WorkoutName} ({workout.ProgramName}). Время для продуктивной тренировки!",
                    NotificationCategory.UpcomingWorkout,
                    todayWorkoutTime));
            }
            else if (tomorrowPlannedWorkouts.Count > 0)
            {
                var tomorrowWorkout = tomorrowPlannedWorkouts[0];
                DateTime tomorrowWorkoutTime = now.Date.AddDays(1) + settings.UpcomingWorkoutTime;
                items.Add(new NotificationItem(
                    NotificationIds.UpcomingWorkoutId,
                    "🏋️ Запланирована тренировка!",
                    $"Завтра по плану: {tomorrowWorkout.WorkoutName} ({tomorrowWorkout.ProgramName}). Будь готов к тренировке!",
                    NotificationCategory.UpcomingWorkout,
                    tomorrowWorkoutTime));
            }
        }

        // 2. Missed Workout Alert
        if (settings.EnableMissedWorkoutAlerts && todayPlannedWorkouts.Count > 0 && !hasFinishedWorkoutToday)
        {
            DateTime missedWorkoutTime = now.Date + settings.MissedWorkoutTime;
            if (now <= missedWorkoutTime)
            {
                var workout = todayPlannedWorkouts[0];
                items.Add(new NotificationItem(
                    NotificationIds.MissedWorkoutId,
                    "⚠️ Пропущена тренировка",
                    $"Сегодня была запланирована тренировка «{workout.WorkoutName}». Если ещё есть время — проведи занятие или перенеси его в календаре.",
                    NotificationCategory.MissedWorkout,
                    missedWorkoutTime));
            }
        }

        // 3. Weight Measurement Reminder
        if (settings.EnableWeightReminders)
        {
            DateTime todayWeightTime = now.Date + settings.WeightReminderTime;
            if (todayMeasurement == null && now <= todayWeightTime)
            {
                items.Add(new NotificationItem(
                    NotificationIds.WeightReminderId,
                    "⚖️ Замер веса",
                    "Утренний замер веса натощак помогает точно отслеживать динамику. Зафиксируй свой вес!",
                    NotificationCategory.WeightMeasurement,
                    todayWeightTime));
            }
            else
            {
                DateTime tomorrowWeightTime = now.Date.AddDays(1) + settings.WeightReminderTime;
                items.Add(new NotificationItem(
                    NotificationIds.WeightReminderId,
                    "⚖️ Замер веса",
                    "Утренний замер веса натощак помогает точно отслеживать динамику. Зафиксируй свой вес!",
                    NotificationCategory.WeightMeasurement,
                    tomorrowWeightTime));
            }
        }

        return items;
    }
}
