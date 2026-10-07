using SQLite;

namespace Myonic.Data.Migrations;

internal static class Migrator
{
    public static async Task MigrateAsync(SQLiteAsyncConnection db)
    {
        var current = await db.ExecuteScalarAsync<int>("PRAGMA user_version");

        foreach (var migration in Migrations.All.Where(m => m.Version > current).OrderBy(m => m.Version))
        {
            // Каждая миграция атомарна: либо применилась целиком, либо откатилась
            await db.RunInTransactionAsync(conn =>
            {
                foreach (var sql in migration.Statements)
                    conn.Execute(sql);

                conn.Execute($"PRAGMA user_version = {migration.Version}");
            });
        }
    }
}