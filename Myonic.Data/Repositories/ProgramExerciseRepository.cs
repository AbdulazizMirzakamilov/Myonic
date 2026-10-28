using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Data.Repositories;

public sealed class ProgramExerciseRepository : IProgramExerciseRepository
{
    private readonly MyonicDatabase _database;

    public ProgramExerciseRepository(MyonicDatabase database) => _database = database;

    public async Task<IReadOnlyList<ProgramExerciseDetails>> GetByWorkoutAsync(int programWorkoutId)
    {
        var db = await _database.GetConnectionAsync();
        return await db.QueryAsync<ProgramExerciseDetails>(
            """
            SELECT pe.Id, pe.ProgramWorkoutId, pe.ExerciseId, e.Name AS ExerciseName,
                   pe."Order" AS "Order", pe.Sets, pe.Reps, pe.RepsMax, pe.Weight, pe.Rpe, pe.RpeMax,
                   pe.RestSeconds, pe.Notes
            FROM ProgramExercise pe
            JOIN Exercise e ON e.Id = pe.ExerciseId
            WHERE pe.ProgramWorkoutId = ?
            ORDER BY pe."Order"
            """, programWorkoutId);
    }

    public async Task<ProgramExercise?> GetAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        return await db.FindAsync<ProgramExercise>(id);
    }

    public async Task<int> AddAsync(ProgramExercise item)
    {
        Validate(item);

        var db = await _database.GetConnectionAsync();
        var maxOrder = await db.ExecuteScalarAsync<int>(
            """SELECT COALESCE(MAX("Order"), 0) FROM ProgramExercise WHERE ProgramWorkoutId = ?""",
            item.ProgramWorkoutId);

        item.Order = maxOrder + 1;
        await db.InsertAsync(item);
        return item.Id;
    }

    public async Task UpdateAsync(ProgramExercise item)
    {
        Validate(item);

        var db = await _database.GetConnectionAsync();
        await db.UpdateAsync(item);
    }

    public async Task DeleteAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        await db.ExecuteAsync("DELETE FROM ProgramExercise WHERE Id = ?", id);
    }

    public async Task MoveAsync(int id, int direction)
    {
        var db = await _database.GetConnectionAsync();

        var item = await db.FindAsync<ProgramExercise>(id);
        if (item is null) return;

        var siblings = await db.Table<ProgramExercise>()
            .Where(x => x.ProgramWorkoutId == item.ProgramWorkoutId)
            .OrderBy(x => x.Order)
            .ToListAsync();

        var index = siblings.FindIndex(x => x.Id == id);
        var target = index + Math.Sign(direction);
        if (target < 0 || target >= siblings.Count) return;

        (siblings[index], siblings[target]) = (siblings[target], siblings[index]);

        // Перенумеровываем целиком: порядок всегда 1..N без дыр
        await db.RunInTransactionAsync(conn =>
        {
            for (var i = 0; i < siblings.Count; i++)
            {
                siblings[i].Order = i + 1;
                conn.Update(siblings[i]);
            }
        });
    }

    private static void Validate(ProgramExercise item)
    {
        if (item.Sets <= 0)
            throw new ArgumentException("Количество подходов должно быть больше нуля.");

        if (item.Reps <= 0)
            throw new ArgumentException("Количество повторений должно быть больше нуля.");

        if (item.RepsMax is not null && item.RepsMax <= item.Reps)
            throw new ArgumentException("Максимальное количество повторений должно быть строго больше минимального.");

        if (item.Weight is < 0)
            throw new ArgumentException("Вес не может быть отрицательным.");

        if (item.Rpe is not null && !IsValidRpe(item.Rpe.Value))
            throw new ArgumentException("RPE должен быть от 5.5 до 10 с шагом 0.5 (например: 6, 6.5, 7, 7.5...).");

        if (item.RpeMax is not null)
        {
            if (item.Rpe is null)
                throw new ArgumentException("Нельзя указать верхний RPE без нижнего.");

            if (!IsValidRpe(item.RpeMax.Value))
                throw new ArgumentException("Верхний RPE должен быть от 5.5 до 10 с шагом 0.5.");

            if (item.RpeMax <= item.Rpe)
                throw new ArgumentException("Верхний RPE должен быть строго больше нижнего.");
        }

        if (item.RestSeconds is < 0)
            throw new ArgumentException("Время отдыха не может быть отрицательным.");
    }

    private static bool IsValidRpe(double rpe)
    {
        if (rpe is < 5.5 or > 10.0) return false;
        var doubled = rpe * 2.0;
        return Math.Abs(doubled - Math.Round(doubled)) < 1e-6;
    }
}
