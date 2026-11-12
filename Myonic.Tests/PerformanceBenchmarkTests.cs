using System.Diagnostics;
using Myonic.Core.Calendar;
using Myonic.Core.Models;
using Myonic.Data;
using Myonic.Data.Repositories;
using Myonic.Data.Services;
using Xunit.Abstractions;

namespace Myonic.Tests;

public class PerformanceBenchmarkTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"myonic-bench-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private WorkoutSessionRepository _sessionRepo = null!;
    private WorkoutScheduleRepository _scheduleRepo = null!;
    private CalendarService _calendarService = null!;
    private DataBackupService _backupService = null!;

    public PerformanceBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _db = new MyonicDatabase(_dbPath);
        _sessionRepo = new WorkoutSessionRepository(_db);
        _scheduleRepo = new WorkoutScheduleRepository(_db);
        _calendarService = new CalendarService(_sessionRepo, _scheduleRepo);
        _backupService = new DataBackupService(_db);

        var conn = await _db.GetConnectionAsync();

        // Seed sample data: 1 program, 3 program workouts, 10 exercises
        var program = new TrainingProgram { Name = "Hypertrophy Block", IsArchived = false };
        await conn.InsertAsync(program);

        var exercises = new List<Exercise>();
        for (int i = 1; i <= 10; i++)
        {
            var ex = new Exercise
            {
                Name = $"Exercise #{i}",
                Type = i % 2 == 0 ? ExerciseType.Compound : ExerciseType.Isolation,
                MuscleGroup = (MuscleGroup)(i % 11 + 1)
            };
            await conn.InsertAsync(ex);
            exercises.Add(ex);
        }

        // Seed 20 workout sessions each with 5 exercises and 4 sets = 400 sets total
        var rnd = new Random(42);
        for (int s = 1; s <= 20; s++)
        {
            var session = new WorkoutSession
            {
                Title = $"Workout Session #{s}",
                StartedAt = DateTime.Today.AddDays(-s * 2),
                FinishedAt = DateTime.Today.AddDays(-s * 2).AddMinutes(60)
            };
            await conn.InsertAsync(session);

            for (int e = 0; e < 5; e++)
            {
                var we = new WorkoutExercise
                {
                    WorkoutSessionId = session.Id,
                    ExerciseId = exercises[e].Id,
                    Order = e + 1
                };
                await conn.InsertAsync(we);

                for (int r = 1; r <= 4; r++)
                {
                    await conn.InsertAsync(new SetEntry
                    {
                        WorkoutExerciseId = we.Id,
                        SetNumber = r,
                        Weight = 60 + rnd.Next(0, 50),
                        Reps = 8 + rnd.Next(0, 5),
                        Rpe = 7.0 + rnd.Next(0, 3) * 0.5,
                        CompletedAt = session.StartedAt.AddMinutes(10 * e + 2 * r)
                    });
                }
            }
        }
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Benchmark_GetSessionDetailAsync_ExecutionTime()
    {
        // Warm-up
        var detail0 = await _sessionRepo.GetSessionDetailAsync(1);
        Assert.NotNull(detail0);

        var sw = Stopwatch.StartNew();
        int iterations = 50;
        for (int i = 0; i < iterations; i++)
        {
            int sessionId = (i % 20) + 1;
            var detail = await _sessionRepo.GetSessionDetailAsync(sessionId);
            Assert.NotNull(detail);
            Assert.Equal(5, detail.Exercises.Count);
        }
        sw.Stop();

        double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
        _output.WriteLine($"[BENCHMARK] GetSessionDetailAsync (Batched Sets): Total {sw.ElapsedMilliseconds} ms, Average per call: {avgMs:F2} ms");
        Assert.True(avgMs < 10.0, $"Batched query should average < 10ms, but was {avgMs}ms");
    }

    [Fact]
    public async Task Benchmark_CalendarIndicators_QueryTime()
    {
        var sw = Stopwatch.StartNew();
        int iterations = 30;
        var start = DateTime.Today.AddMonths(-1);
        var end = DateTime.Today.AddMonths(2);

        for (int i = 0; i < iterations; i++)
        {
            var indicators = await _calendarService.GetIndicatorsForRangeAsync(start, end);
            Assert.NotEmpty(indicators);
        }
        sw.Stop();

        double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
        _output.WriteLine($"[BENCHMARK] GetIndicatorsForRangeAsync (Indexed): Total {sw.ElapsedMilliseconds} ms, Average per call: {avgMs:F2} ms");
        Assert.True(avgMs < 15.0, $"Indexed calendar query should average < 15ms, but was {avgMs}ms");
    }

    [Fact]
    public async Task Benchmark_StreamingJsonExport_ExecutionTimeAndMemory()
    {
        var exportPath = Path.Combine(Path.GetTempPath(), $"myonic-export-bench-{Guid.NewGuid():N}.json");
        try
        {
            long memBefore = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();

            await _backupService.ExportToJsonAsync(exportPath);

            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            long memDelta = Math.Max(0, memAfter - memBefore);

            _output.WriteLine($"[BENCHMARK] ExportToJsonAsync (Streaming): Elapsed {sw.ElapsedMilliseconds} ms, Memory delta: {memDelta / 1024} KB");
            Assert.True(File.Exists(exportPath));
            var fileInfo = new FileInfo(exportPath);
            Assert.True(fileInfo.Length > 0);
        }
        finally
        {
            if (File.Exists(exportPath))
                File.Delete(exportPath);
        }
    }
}
