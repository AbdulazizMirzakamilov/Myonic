namespace Myonic.Data.Migrations;

internal static class AddRepAndRpeRanges
{
    public static readonly string[] Statements =
    [
        "ALTER TABLE ProgramExercise ADD COLUMN RepsMax INTEGER",
        "ALTER TABLE ProgramExercise ADD COLUMN RpeMax REAL",
        "ALTER TABLE WorkoutExercise ADD COLUMN PlannedRepsMax INTEGER",
        "ALTER TABLE WorkoutExercise ADD COLUMN PlannedRpeMax REAL"
    ];
}
