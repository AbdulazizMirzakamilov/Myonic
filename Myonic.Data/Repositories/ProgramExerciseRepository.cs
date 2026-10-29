using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Core.Validation;

namespace Myonic.Data.Repositories;

public sealed class ProgramExerciseRepository : IProgramExerciseRepository
{
    private readonly MyonicDatabase _database;

    public ProgramExerciseRepository(MyonicDatabase database) => _database = database;

    public async Task<IReadOnlyList<ProgramExerciseDetail>> GetByWorkoutIdAsync(int workoutId)
    {
        var db = await _database.GetConnectionAsync();
        return await db.QueryAsync<ProgramExerciseDetail>(
            """
            SELECT pe.Id, pe.ProgramWorkoutId, pe.ExerciseId, e.Name AS ExerciseName, e.Type AS ExerciseType,
                   pe."Order", pe.Sets, pe.Reps, pe.RepsMax, pe.Weight, pe.Rpe, pe.RpeMax, pe.RestSeconds, pe.Notes
            FROM ProgramExercise pe
            INNER JOIN Exercise e ON e.Id = pe.ExerciseId
            WHERE pe.ProgramWorkoutId = ?
            ORDER BY pe."Order" ASC
            """,
            workoutId);
    }

    public async Task<ProgramExercise?> GetByIdAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        return await db.FindAsync<ProgramExercise>(id);
    }

    public async Task<int> AddAsync(ProgramExercise exercise)
    {
        ProgramExerciseValidator.Validate(exercise);

        var db = await _database.GetConnectionAsync();

        if (exercise.Order <= 0)
        {
            var maxOrder = await db.ExecuteScalarAsync<int>(
                "SELECT IFNULL(MAX(\"Order\"), 0) FROM ProgramExercise WHERE ProgramWorkoutId = ?",
                exercise.ProgramWorkoutId);
            exercise.Order = maxOrder + 1;
        }

        await db.InsertAsync(exercise);
        return exercise.Id;
    }

    public async Task UpdateAsync(ProgramExercise exercise)
    {
        ProgramExerciseValidator.Validate(exercise);

        var db = await _database.GetConnectionAsync();
        await db.UpdateAsync(exercise);
    }

    public async Task DeleteAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        var item = await db.FindAsync<ProgramExercise>(id);
        if (item == null) return;

        await db.DeleteAsync(item);

        // Recompact order
        var remaining = await db.Table<ProgramExercise>()
            .Where(e => e.ProgramWorkoutId == item.ProgramWorkoutId)
            .OrderBy(e => e.Order)
            .ToListAsync();

        for (int i = 0; i < remaining.Count; i++)
        {
            var expectedOrder = i + 1;
            if (remaining[i].Order != expectedOrder)
            {
                remaining[i].Order = expectedOrder;
                await db.UpdateAsync(remaining[i]);
            }
        }
    }

    public async Task MoveUpAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        var current = await db.FindAsync<ProgramExercise>(id);
        if (current == null) return;

        var prev = await db.Table<ProgramExercise>()
            .Where(e => e.ProgramWorkoutId == current.ProgramWorkoutId && e.Order < current.Order)
            .OrderByDescending(e => e.Order)
            .FirstOrDefaultAsync();

        if (prev == null) return;

        var temp = current.Order;
        current.Order = prev.Order;
        prev.Order = temp;

        await db.UpdateAsync(current);
        await db.UpdateAsync(prev);
    }

    public async Task MoveDownAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        var current = await db.FindAsync<ProgramExercise>(id);
        if (current == null) return;

        var next = await db.Table<ProgramExercise>()
            .Where(e => e.ProgramWorkoutId == current.ProgramWorkoutId && e.Order > current.Order)
            .OrderBy(e => e.Order)
            .FirstOrDefaultAsync();

        if (next == null) return;

        var temp = current.Order;
        current.Order = next.Order;
        next.Order = temp;

        await db.UpdateAsync(current);
        await db.UpdateAsync(next);
    }
}
