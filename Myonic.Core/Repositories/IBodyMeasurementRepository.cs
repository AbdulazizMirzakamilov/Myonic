using Myonic.Core.Models;

namespace Myonic.Core.Repositories;

public interface IBodyMeasurementRepository
{
    Task<int> AddAsync(BodyMeasurement measurement);
    Task UpdateAsync(BodyMeasurement measurement);
    Task DeleteAsync(int id);
    Task<BodyMeasurement?> GetByIdAsync(int id);
    Task<BodyMeasurement?> GetLatestAsync();
    Task<BodyMeasurement?> GetByDateAsync(DateTime date);
    Task<List<BodyMeasurement>> GetAllAsync(int limit = 50);
    Task<List<BodyMeasurement>> GetWeightHistoryAsync(DateTime? fromDate = null, int limit = 60);
    Task<WeightTrendStats> GetWeightTrendStatsAsync(int days = 30);
}
