using Myonic.Core.Models;

namespace Myonic.Core.Repositories;

public interface IGoalRepository
{
    Task<int> AddAsync(Goal goal);
    Task UpdateAsync(Goal goal);
    Task DeleteAsync(int id);
    Task<Goal?> GetByIdAsync(int id);
    Task<IReadOnlyList<Goal>> GetActiveGoalsAsync();
    Task<IReadOnlyList<Goal>> GetAllGoalsAsync();
    Task SyncCurrentValuesAsync();
}
