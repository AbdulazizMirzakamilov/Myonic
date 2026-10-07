using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Data.Repositories;

public sealed class ExerciseRepository : IExerciseRepository
{
    private readonly MyonicDatabase _database;

    public ExerciseRepository(MyonicDatabase database) => _database = database;

    public async Task<IReadOnlyList<Exercise>> GetAllAsync(string? search = null, bool includeArchived = false)
    {
        var db = await _database.GetConnectionAsync();

        var query = db.Table<Exercise>();
        if (!includeArchived)
            query = query.Where(e => e.IsArchived == false);

        var items = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            items = items.Where(e => e.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return items.OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task<Exercise?> GetByIdAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        return await db.FindAsync<Exercise>(id);
    }

    public async Task<int> AddAsync(Exercise exercise)
    {
        exercise.Name = NormalizeName(exercise.Name);
        await EnsureNameIsUniqueAsync(exercise.Name, exceptId: 0);

        exercise.IsCustom = true;
        exercise.IsArchived = false;

        var db = await _database.GetConnectionAsync();
        await db.InsertAsync(exercise);
        return exercise.Id;
    }

    public async Task UpdateAsync(Exercise exercise)
    {
        exercise.Name = NormalizeName(exercise.Name);
        await EnsureNameIsUniqueAsync(exercise.Name, exceptId: exercise.Id);

        var db = await _database.GetConnectionAsync();
        await db.UpdateAsync(exercise);
    }

    public async Task<ExerciseRemoval> RemoveAsync(int id)
    {
        var db = await _database.GetConnectionAsync();

        var used = await db.ExecuteScalarAsync<int>(
            """
            SELECT EXISTS (
                SELECT 1 FROM ProgramExercise WHERE ExerciseId = ?
                UNION ALL SELECT 1 FROM WorkoutExercise WHERE ExerciseId = ?
                UNION ALL SELECT 1 FROM PersonalRecord WHERE ExerciseId = ?
            )
            """, id, id, id) > 0;

        if (used)
        {
            await db.ExecuteAsync("UPDATE Exercise SET IsArchived = 1 WHERE Id = ?", id);
            return ExerciseRemoval.Archived;
        }

        // Внешние ключи (RESTRICT) подстрахуют, если между проверкой и удалением что-то изменится
        await db.ExecuteAsync("DELETE FROM Exercise WHERE Id = ?", id);
        return ExerciseRemoval.Deleted;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название упражнения не может быть пустым.", nameof(name));
        return name.Trim();
    }

    private async Task EnsureNameIsUniqueAsync(string name, int exceptId)
    {
        var existing = await GetAllAsync();
        if (existing.Any(e => e.Id != exceptId && string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Упражнение «{name}» уже существует.");
    }
}