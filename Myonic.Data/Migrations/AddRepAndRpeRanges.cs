namespace Myonic.Data.Migrations;

public static class AddRepAndRpeRanges
{
    public static readonly IReadOnlyList<string> Statements =
    [
        "ALTER TABLE ProgramExercise ADD COLUMN RepsMax INTEGER CHECK (RepsMax IS NULL OR RepsMax > 0);",
        "ALTER TABLE ProgramExercise ADD COLUMN RpeMax REAL CHECK (RpeMax IS NULL OR (RpeMax BETWEEN 1 AND 10));",
        "ALTER TABLE WorkoutExercise ADD COLUMN PlannedRepsMax INTEGER CHECK (PlannedRepsMax IS NULL OR PlannedRepsMax > 0);",
        "ALTER TABLE WorkoutExercise ADD COLUMN PlannedRpeMax REAL CHECK (PlannedRpeMax IS NULL OR (PlannedRpeMax BETWEEN 1 AND 10));"
    ];
}
