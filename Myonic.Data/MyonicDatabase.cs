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
        typeof(WorkoutExercise), typeof(SetEntry), typeof(PersonalRecord),

        typeof(BodyMeasurement),
        typeof(Goal)
    ];

    private readonly string _path;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    public MyonicDatabase(string databasePath) => _path = databasePath;

    public string DatabasePath => _path;

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
            await connection.ExecuteScalarAsync<string>("PRAGMA journal_mode = WAL");

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

    /// <summary>
    /// Создает атомарную резервную копию базы данных без блокировки и риска повреждения данных.
    /// </summary>
    public async Task BackupAsync(string destinationPath)
    {
        var conn = await GetConnectionAsync();
        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        await conn.ExecuteAsync("VACUUM INTO ?", destinationPath);
    }

    public async Task CloseConnectionAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                await _connection.CloseAsync();
                _connection = null;
            }
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
