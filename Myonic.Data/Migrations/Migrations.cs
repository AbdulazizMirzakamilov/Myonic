namespace Myonic.Data.Migrations;

public static class Migrations
{
    public static readonly IReadOnlyList<Migration> All =
    [
        new Migration(1, "Initial schema", InitialSchema.Statements),
        new Migration(2, "Seed default exercises", SeedExercises.Statements),
        new Migration(3, "Add rep and rpe ranges", AddRepAndRpeRanges.Statements),
        new Migration(4, "Add reps to personal record", AddRepsToPersonalRecord.Statements)
    ];

    public static int LatestVersion => All.Max(m => m.Version);
}
