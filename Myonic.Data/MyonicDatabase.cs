// MyonicDatabase.cs
using Myonic.Core.Models;
using Myonic.Data.Migrations;
using SQLite;

namespace Myonic.Data;

public sealed class MyonicDatabase : IAsyncDisposable
{
    private static readonly Type[] ModelTypes =
    [
        typeof(Exercise), typeof(TrainingProgram), typeof(ProgramWorkout),
        typeof(ProgramExercise), typeof(ScheduledWorkout), typeof(WorkoutSession),
        typeof(WorkoutExercise), typeof(SetEntry), typeof(PersonalRecord)
    ];

    private readonly string _path;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    public MyonicDatabase(string databasePath) => _path = databasePath;

    /// <summary>Открывает базу при первом обращении и применяет миграции.</summary>
    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null) return _connection;

        await _initLock.WaitAsync();
        try
        {
            if (_connection is not null) return _connection;

            // false = даты хранятся читаемым текстом, а не тиками
            var connection = new SQLiteAsyncConnection(_path, storeDateTimeAsTicks: false);

            await connection.ExecuteAsync("PRAGMA foreign_keys = ON");

            await Migrator.MigrateAsync(connection);

            foreach (var type in ModelTypes)
                await connection.CreateTableAsync(type, CreateFlags.ImplicitPK | CreateFlags.AutoIncPK);

            _connection = connection;
            return connection;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection = null;
        }
    }
}