namespace Myonic.Data.Migrations;

public static class AddRepsToPersonalRecord
{
    public static readonly IReadOnlyList<string> Statements =
    [
        "ALTER TABLE PersonalRecord ADD COLUMN Reps INTEGER NOT NULL DEFAULT 1;"
    ];
}
