using Myonic.Core.Models;
using Myonic.Core.Repositories;

namespace Myonic.Data.Repositories;

public sealed class TrainingProgramRepository : ITrainingProgramRepository
{
    private readonly MyonicDatabase _database;

    public TrainingProgramRepository(MyonicDatabase database) => _database = database;

    public async Task<IReadOnlyList<ProgramSummary>> GetSummariesAsync()
    {
        var db = await _database.GetConnectionAsync();

        var items = await db.QueryAsync<ProgramSummary>(
            """
            SELECT p.Id, p.Name, p.Description,
                   (SELECT COUNT(*) FROM ProgramWorkout w WHERE w.ProgramId = p.Id) AS WorkoutCount
            FROM TrainingProgram p
            WHERE p.IsArchived = 0
            """);

        // Сортировка в C#, потому что ORDER BY в SQLite не умеет кириллицу без учёта регистра
        return items.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task<TrainingProgram?> GetProgramAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        return await db.FindAsync<TrainingProgram>(id);
    }

    public async Task<int> AddProgramAsync(TrainingProgram program)
    {
        program.Name = NormalizeName(program.Name, "Название программы");
        await EnsureProgramNameIsUniqueAsync(program.Name, exceptId: 0);

        var db = await _database.GetConnectionAsync();
        await db.InsertAsync(program);
        return program.Id;
    }

    public async Task UpdateProgramAsync(TrainingProgram program)
    {
        program.Name = NormalizeName(program.Name, "Название программы");
        await EnsureProgramNameIsUniqueAsync(program.Name, exceptId: program.Id);

        var db = await _database.GetConnectionAsync();
        await db.UpdateAsync(program);
    }

    public async Task DeleteProgramAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        // Тренировки и расписание удалятся каскадом, история сохранится
        await db.ExecuteAsync("DELETE FROM TrainingProgram WHERE Id = ?", id);
    }

    public async Task<IReadOnlyList<ProgramWorkout>> GetWorkoutsAsync(int programId)
    {
        var db = await _database.GetConnectionAsync();
        return await db.Table<ProgramWorkout>()
            .Where(w => w.ProgramId == programId)
            .OrderBy(w => w.Order)
            .ToListAsync();
    }

    public async Task<ProgramWorkout?> GetWorkoutAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        return await db.FindAsync<ProgramWorkout>(id);
    }

    public async Task<int> AddWorkoutAsync(ProgramWorkout workout)
    {
        workout.Name = NormalizeName(workout.Name, "Название тренировки");

        var db = await _database.GetConnectionAsync();
        var maxOrder = await db.ExecuteScalarAsync<int>(
            """SELECT COALESCE(MAX("Order"), 0) FROM ProgramWorkout WHERE ProgramId = ?""",
            workout.ProgramId);

        workout.Order = maxOrder + 1;
        await db.InsertAsync(workout);
        return workout.Id;
    }

    public async Task UpdateWorkoutAsync(ProgramWorkout workout)
    {
        workout.Name = NormalizeName(workout.Name, "Название тренировки");

        var db = await _database.GetConnectionAsync();
        await db.UpdateAsync(workout);
    }

    public async Task DeleteWorkoutAsync(int id)
    {
        var db = await _database.GetConnectionAsync();
        await db.ExecuteAsync("DELETE FROM ProgramWorkout WHERE Id = ?", id);
    }

    private static string NormalizeName(string name, string fieldTitle)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"{fieldTitle} не может быть пустым.", nameof(name));
        return name.Trim();
    }

    private async Task EnsureProgramNameIsUniqueAsync(string name, int exceptId)
    {
        var existing = await GetSummariesAsync();
        if (existing.Any(p => p.Id != exceptId && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Программа «{name}» уже существует.");
    }
}