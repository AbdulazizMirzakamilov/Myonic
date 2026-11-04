using Myonic.Core.Calendar;
using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Data.Services;

public sealed class CalendarService : ICalendarService
{
    private readonly IWorkoutSessionRepository _sessionRepo;
    private readonly IWorkoutScheduleRepository _scheduleRepo;

    public CalendarService(
        IWorkoutSessionRepository sessionRepo,
        IWorkoutScheduleRepository scheduleRepo)
    {
        _sessionRepo = sessionRepo;
        _scheduleRepo = scheduleRepo;
    }

    public async Task<IReadOnlyList<CalendarWorkoutItem>> GetWorkoutsForDateAsync(DateTime date)
    {
        var targetDate = date.Date;
        var today = DateTime.Today;
        var result = new List<CalendarWorkoutItem>();

        // 1. Completed sessions for this date
        var completedSessions = await _sessionRepo.GetSessionsForDateAsync(targetDate);
        var completedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var session in completedSessions)
        {
            var title = string.IsNullOrWhiteSpace(session.Title) ? "Тренировка" : session.Title.Trim();
            completedTitles.Add(title);

            var timeText = session.StartedAt.ToLocalTime().ToString("HH:mm");
            var durationText = string.Empty;
            if (session.FinishedAt.HasValue)
            {
                var duration = session.FinishedAt.Value - session.StartedAt;
                var minutes = (int)Math.Max(1, Math.Round(duration.TotalMinutes));
                durationText = $"{minutes} мин";
            }

            var exercisesCount = session.Exercises?.Count ?? 0;
            var setsCount = session.Exercises?.Sum(e => e.Sets?.Count ?? 0) ?? 0;
            var summaryText = exercisesCount > 0
                ? $"{exercisesCount} упр. • {setsCount} подходов"
                : "Без упражнений";

            var completedExercises = new List<CalendarCompletedExerciseItem>();
            if (session.Exercises != null)
            {
                foreach (var ex in session.Exercises)
                {
                    var setsSummary = ex.Sets?
                        .Select(s => s.Rpe.HasValue
                            ? $"{s.Weight} кг × {s.Reps} @ {s.Rpe.Value:0.#}"
                            : $"{s.Weight} кг × {s.Reps}")
                        .ToList() ?? [];

                    completedExercises.Add(new CalendarCompletedExerciseItem
                    {
                        ExerciseName = ex.ExerciseName,
                        SetsSummary = setsSummary
                    });
                }
            }

            result.Add(new CalendarWorkoutItem
            {
                SessionId = session.Id,
                Title = title,
                TimeText = timeText,
                DurationText = durationText,
                Status = CalendarWorkoutStatus.Completed,
                ExercisesCount = exercisesCount,
                SetsCount = setsCount,
                SummaryText = summaryText,
                CompletedExercises = completedExercises
            });
        }

        // 2. Planned workouts for this date
        var plannedWorkouts = await _scheduleRepo.GetPlannedWorkoutsForDateAsync(targetDate);

        foreach (var p in plannedWorkouts)
        {
            var pTitle = p.WorkoutName?.Trim() ?? string.Empty;

            // Check if this planned workout was completed
            var isCompleted = completedTitles.Any(t =>
                string.Equals(t, pTitle, StringComparison.OrdinalIgnoreCase) ||
                (pTitle.Length > 0 && t.Contains(pTitle, StringComparison.OrdinalIgnoreCase)) ||
                (t.Length > 0 && pTitle.Contains(t, StringComparison.OrdinalIgnoreCase)));

            // If only 1 workout planned and completed sessions exist on this day, correlate them
            if (!isCompleted && plannedWorkouts.Count == 1 && completedSessions.Count > 0)
            {
                isCompleted = true;
            }

            // If already completed, do NOT add a duplicate card
            if (isCompleted)
            {
                continue;
            }

            // Do not show separate cards just because a past planned date was uncompleted
            if (targetDate < today)
            {
                continue;
            }

            var summaryText = p.ExerciseCount > 0
                ? $"{p.ExerciseCount} запланированных упражнений"
                : "План не содержит упражнений";

            result.Add(new CalendarWorkoutItem
            {
                ProgramWorkoutId = p.ProgramWorkoutId,
                Title = pTitle,
                ProgramName = p.ProgramName,
                Status = CalendarWorkoutStatus.Planned,
                ExercisesCount = p.ExerciseCount,
                SummaryText = summaryText,
                PlannedExercises = p.Exercises
            });
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<DateTime, CalendarDayIndicators>> GetIndicatorsForRangeAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var fromUtc = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Local).ToUniversalTime();
        var toUtc = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();

        // 1. Completed dates in range
        var completedDates = (await _sessionRepo.GetCompletedSessionDatesAsync(fromUtc, toUtc))
            .Select(d => d.ToLocalTime().Date)
            .ToHashSet();

        // 2. Planned day indicators
        var (defaultDays, scheduledDates) = await _scheduleRepo.GetPlannedDayIndicatorsForWeekAsync(fromDate, toDate);

        var today = DateTime.Today;
        var indicators = new Dictionary<DateTime, CalendarDayIndicators>();

        for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
        {
            var hasCompleted = completedDates.Contains(date);
            var isPlannedDay = scheduledDates.Contains(date) || defaultDays.Contains(date.DayOfWeek);

            // A day has planned workout if isPlannedDay is true, not completed yet, and date >= today
            var hasPlanned = isPlannedDay && !hasCompleted && date >= today;

            indicators[date] = new CalendarDayIndicators
            {
                HasCompleted = hasCompleted,
                HasPlanned = hasPlanned
            };
        }

        return indicators;
    }
}
