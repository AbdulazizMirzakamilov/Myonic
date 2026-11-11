namespace Myonic.Data.Migrations;

public static class AddBodyMeasurementsAndGoals
{
    public static readonly IReadOnlyList<string> Statements =
    [
        """
        CREATE TABLE IF NOT EXISTS BodyMeasurement (
            Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
            Date               TEXT    NOT NULL,
            WeightKg           REAL    NOT NULL,
            BodyFatPercentage  REAL,
            NeckCm             REAL,
            ChestCm            REAL,
            WaistCm            REAL,
            HipsCm             REAL,
            BicepsCm           REAL,
            ThighCm            REAL,
            CalfCm             REAL,
            Notes              TEXT,
            CreatedAt          TEXT    NOT NULL
        )
        """,
        "CREATE INDEX IF NOT EXISTS IX_BodyMeasurement_Date ON BodyMeasurement(Date)",

        """
        CREATE TABLE IF NOT EXISTS Goal (
            Id           INTEGER PRIMARY KEY AUTOINCREMENT,
            Title        TEXT    NOT NULL,
            Type         INTEGER NOT NULL,
            ExerciseId   INTEGER REFERENCES Exercise(Id) ON DELETE SET NULL,
            ExerciseName TEXT,
            BodyPart     TEXT,
            StartValue   REAL    NOT NULL,
            TargetValue  REAL    NOT NULL,
            CurrentValue REAL    NOT NULL,
            TargetDate   TEXT,
            Status       INTEGER NOT NULL DEFAULT 0,
            Notes        TEXT,
            CreatedAt    TEXT    NOT NULL,
            AchievedAt   TEXT
        )
        """,
        "CREATE INDEX IF NOT EXISTS IX_Goal_Status ON Goal(Status)",
        "CREATE INDEX IF NOT EXISTS IX_Goal_ExerciseId ON Goal(ExerciseId)"
    ];
}
