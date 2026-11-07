using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Data.Repositories;

public sealed class BodyMeasurementRepository : IBodyMeasurementRepository
{
    private readonly MyonicDatabase _database;

    public BodyMeasurementRepository(MyonicDatabase database) => _database = database;

    public async Task<int> AddAsync(BodyMeasurement measurement)
    {
        measurement.CreatedAt = DateTime.UtcNow;
        var db = await _database.GetConnectionAsync();
        await db.InsertAsync(measurement);
        return measurement.Id;
    }

    public async Task UpdateAsync(BodyMeasurement measurement)
    {
        var db = await _database.GetConnectionAsync();
        await db.UpdateAsync(measurement);
    }

    public async Task DeleteAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        await db.DeleteAsync<BodyMeasurement>(id);
    }

    public async Task<BodyMeasurement?> GetByIdAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        return await db.FindAsync<BodyMeasurement>(id);
    }

    public async Task<BodyMeasurement?> GetLatestAsync()
    {
        var db = await _database.GetConnectionAsync();
        return await db.Table<BodyMeasurement>()
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync();
    }

    public async Task<BodyMeasurement?> GetByDateAsync(DateTime date)
    {
        var db = await _database.GetConnectionAsync();
        var dateStr = date.Date.ToString("yyyy-MM-dd");
        var results = await db.QueryAsync<BodyMeasurement>(
            "SELECT * FROM BodyMeasurement WHERE date(Date) = ? ORDER BY Date DESC LIMIT 1",
            dateStr);
        return results.FirstOrDefault();
    }

    public async Task<List<BodyMeasurement>> GetAllAsync(int limit = 50)
    {
        var db = await _database.GetConnectionAsync();
        return await db.Table<BodyMeasurement>()
            .OrderByDescending(m => m.Date)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<BodyMeasurement>> GetWeightHistoryAsync(DateTime? fromDate = null, int limit = 60)
    {
        var db = await _database.GetConnectionAsync();

        if (fromDate.HasValue)
        {
            var from = fromDate.Value;
            return await db.Table<BodyMeasurement>()
                .Where(m => m.Date >= from)
                .OrderBy(m => m.Date)
                .Take(limit)
                .ToListAsync();
        }

        return await db.Table<BodyMeasurement>()
            .OrderByDescending(m => m.Date)
            .Take(limit)
            .ToListAsync()
            .ContinueWith(t => t.Result.OrderBy(m => m.Date).ToList());
    }

    public async Task<WeightTrendStats> GetWeightTrendStatsAsync(int days = 30)
    {
        var db = await _database.GetConnectionAsync();
        var fromDate = DateTime.Today.AddDays(-days);

        var measurements = await db.Table<BodyMeasurement>()
            .Where(m => m.Date >= fromDate && m.WeightKg > 0)
            .OrderBy(m => m.Date)
            .ToListAsync();

        if (measurements.Count == 0)
        {
            return new WeightTrendStats(0, 0, 0, 0, 0, 0);
        }

        var current = measurements.Last().WeightKg;
        var start = measurements.First().WeightKg;
        var delta = Math.Round(current - start, 1);
        var min = measurements.Min(m => m.WeightKg);
        var max = measurements.Max(m => m.WeightKg);

        return new WeightTrendStats(current, start, delta, min, max, measurements.Count);
    }
}
