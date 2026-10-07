namespace Myonic.Data.Migrations;

public static class Migrations
{
    public static readonly IReadOnlyList<Migration> All =
    [
        new Migration(1, "Initial schema", InitialSchema.Statements)
    ];

    public static int LatestVersion => All.Max(m => m.Version);
}