using Myonic.Core.Models;

namespace Myonic.Core.Repositories;

public enum ExerciseRemoval
{
    Deleted,    // удалено полностью
    Archived    // есть в программах или истории, скрыто
}

public interface IExerciseRepository
{
    Task<IReadOnlyList<Exercise>> GetAllAsync(string? search = null, bool includeArchived = false);
    Task<Exercise?> GetByIdAsync(int id);

    /// <summary>Добавляет пользовательское упражнение, возвращает его Id.</summary>
    Task<int> AddAsync(Exercise exercise);
    Task UpdateAsync(Exercise exercise);
    Task<ExerciseRemoval> RemoveAsync(int id);
}