using Myonic.Core.Models;
using Myonic.Data;
using Myonic.Data.Repositories;

namespace Myonic.Tests;

public class BodyMeasurementRepositoryTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-body-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private BodyMeasurementRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _repo = new BodyMeasurementRepository(_db);
        // Ensure tables are created
        await _db.GetConnectionAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    [Fact]
    public async Task AddAsync_InsertsAndReturnsId()
    {
        var m = new BodyMeasurement { Date = DateTime.Today, WeightKg = 82.5 };
        var id = await _repo.AddAsync(m);

        Assert.True(id > 0);

        var retrieved = await _repo.GetByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.Equal(82.5, retrieved.WeightKg);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingRecord()
    {
        var m = new BodyMeasurement { Date = DateTime.Today, WeightKg = 80.0, WaistCm = 85.0 };
        var id = await _repo.AddAsync(m);

        var existing = await _repo.GetByIdAsync(id);
        Assert.NotNull(existing);
        existing.WeightKg = 79.5;
        existing.WaistCm = 84.0;
        await _repo.UpdateAsync(existing);

        var updated = await _repo.GetByIdAsync(id);
        Assert.NotNull(updated);
        Assert.Equal(79.5, updated.WeightKg);
        Assert.Equal(84.0, updated.WaistCm);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord()
    {
        var m = new BodyMeasurement { Date = DateTime.Today, WeightKg = 81.0 };
        var id = await _repo.AddAsync(m);

        await _repo.DeleteAsync(id);

        var deleted = await _repo.GetByIdAsync(id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsNewestByDate()
    {
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today.AddDays(-7), WeightKg = 83.0 });
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today.AddDays(-3), WeightKg = 82.0 });
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today, WeightKg = 81.5 });

        var latest = await _repo.GetLatestAsync();

        Assert.NotNull(latest);
        Assert.Equal(81.5, latest.WeightKg);
    }

    [Fact]
    public async Task GetLatestAsync_EmptyDb_ReturnsNull()
    {
        var latest = await _repo.GetLatestAsync();
        Assert.Null(latest);
    }

    [Fact]
    public async Task GetByDateAsync_FindsCorrectDay()
    {
        var targetDate = DateTime.Today.AddDays(-2);
        await _repo.AddAsync(new BodyMeasurement { Date = targetDate, WeightKg = 80.0 });
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today, WeightKg = 79.0 });

        var found = await _repo.GetByDateAsync(targetDate);

        Assert.NotNull(found);
        Assert.Equal(80.0, found.WeightKg);
    }

    [Fact]
    public async Task GetByDateAsync_NoMatch_ReturnsNull()
    {
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today, WeightKg = 80.0 });

        var found = await _repo.GetByDateAsync(DateTime.Today.AddDays(-10));
        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDescendingByDate_WithLimit()
    {
        for (int i = 0; i < 5; i++)
        {
            await _repo.AddAsync(new BodyMeasurement
            {
                Date = DateTime.Today.AddDays(-i),
                WeightKg = 80.0 + i
            });
        }

        var all = await _repo.GetAllAsync(limit: 3);

        Assert.Equal(3, all.Count);
        // Descending by date: today first
        Assert.True(all[0].Date >= all[1].Date);
        Assert.True(all[1].Date >= all[2].Date);
    }

    [Fact]
    public async Task GetWeightHistoryAsync_ReturnsAscendingOrder()
    {
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today.AddDays(-5), WeightKg = 83.0 });
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today.AddDays(-2), WeightKg = 82.0 });
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today, WeightKg = 81.0 });

        var history = await _repo.GetWeightHistoryAsync();

        Assert.Equal(3, history.Count);
        // Ascending by date
        Assert.True(history[0].Date <= history[1].Date);
        Assert.True(history[1].Date <= history[2].Date);
    }

    [Fact]
    public async Task GetWeightHistoryAsync_WithFromDate_FiltersOlderEntries()
    {
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today.AddDays(-60), WeightKg = 90.0 });
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today.AddDays(-10), WeightKg = 85.0 });
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today, WeightKg = 82.0 });

        var history = await _repo.GetWeightHistoryAsync(fromDate: DateTime.Today.AddDays(-30));

        Assert.Equal(2, history.Count);
        Assert.DoesNotContain(history, m => m.WeightKg == 90.0);
    }

    [Fact]
    public async Task GetWeightTrendStatsAsync_CalculatesCorrectly()
    {
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today.AddDays(-20), WeightKg = 85.0 });
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today.AddDays(-10), WeightKg = 83.0 });
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today.AddDays(-5), WeightKg = 82.0 });
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today, WeightKg = 81.0 });

        var stats = await _repo.GetWeightTrendStatsAsync(days: 30);

        Assert.Equal(81.0, stats.CurrentWeight);
        Assert.Equal(85.0, stats.StartWeight);
        Assert.Equal(-4.0, stats.DeltaKg);
        Assert.Equal(81.0, stats.MinWeight);
        Assert.Equal(85.0, stats.MaxWeight);
        Assert.Equal(4, stats.MeasurementsCount);
    }

    [Fact]
    public async Task GetWeightTrendStatsAsync_EmptyDb_ReturnsZeros()
    {
        var stats = await _repo.GetWeightTrendStatsAsync();

        Assert.Equal(0, stats.CurrentWeight);
        Assert.Equal(0, stats.DeltaKg);
        Assert.Equal(0, stats.MeasurementsCount);
    }

    [Fact]
    public async Task GetWeightTrendStatsAsync_SingleMeasurement_DeltaIsZero()
    {
        await _repo.AddAsync(new BodyMeasurement { Date = DateTime.Today, WeightKg = 80.0 });

        var stats = await _repo.GetWeightTrendStatsAsync(days: 30);

        Assert.Equal(80.0, stats.CurrentWeight);
        Assert.Equal(80.0, stats.StartWeight);
        Assert.Equal(0.0, stats.DeltaKg);
        Assert.Equal(1, stats.MeasurementsCount);
    }

    [Fact]
    public async Task NullableFields_StoredAndRetrievedCorrectly()
    {
        var m = new BodyMeasurement
        {
            Date = DateTime.Today,
            WeightKg = 82.0,
            BodyFatPercentage = 15.5,
            NeckCm = 38.0,
            ChestCm = 100.0,
            WaistCm = 82.0,
            HipsCm = 95.0,
            BicepsCm = 36.0,
            ThighCm = 58.0,
            CalfCm = 38.5,
            Notes = "Утро, натощак"
        };
        var id = await _repo.AddAsync(m);

        var retrieved = await _repo.GetByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.Equal(15.5, retrieved.BodyFatPercentage);
        Assert.Equal(38.0, retrieved.NeckCm);
        Assert.Equal(100.0, retrieved.ChestCm);
        Assert.Equal(82.0, retrieved.WaistCm);
        Assert.Equal(95.0, retrieved.HipsCm);
        Assert.Equal(36.0, retrieved.BicepsCm);
        Assert.Equal(58.0, retrieved.ThighCm);
        Assert.Equal(38.5, retrieved.CalfCm);
        Assert.Equal("Утро, натощак", retrieved.Notes);
    }

    [Fact]
    public async Task NullableFields_OmittedFields_StayNull()
    {
        var m = new BodyMeasurement { Date = DateTime.Today, WeightKg = 80.0 };
        var id = await _repo.AddAsync(m);

        var retrieved = await _repo.GetByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.Null(retrieved.BodyFatPercentage);
        Assert.Null(retrieved.NeckCm);
        Assert.Null(retrieved.ChestCm);
        Assert.Null(retrieved.Notes);
    }
}
