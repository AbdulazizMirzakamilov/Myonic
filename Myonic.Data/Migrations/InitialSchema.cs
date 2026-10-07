namespace Myonic.Data.Migrations;

internal static class InitialSchema
{
    public static readonly string[] Statements =
    [
        """
        CREATE TABLE Exercise (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            Name        TEXT    NOT NULL,
            Description TEXT,
            Type        INTEGER NOT NULL,
            Notes       TEXT,
            IsCustom    INTEGER NOT NULL DEFAULT 0,
            IsArchived  INTEGER NOT NULL DEFAULT 0
        )
        """,
        """
        CREATE TABLE TrainingProgram (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            Name        TEXT    NOT NULL,
            Description TEXT,
            IsArchived  INTEGER NOT NULL DEFAULT 0
        )
        """,
        """
        CREATE TABLE ProgramWorkout (
            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
            ProgramId       INTEGER NOT NULL REFERENCES TrainingProgram(Id) ON DELETE CASCADE,
            Name            TEXT    NOT NULL,
            "Order"         INTEGER NOT NULL,
            DefaultDayOfWeek INTEGER
        )
        """,
        """
        CREATE TABLE ProgramExercise (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            ProgramWorkoutId INTEGER NOT NULL REFERENCES ProgramWorkout(Id) ON DELETE CASCADE,
            ExerciseId       INTEGER NOT NULL REFERENCES Exercise(Id) ON DELETE RESTRICT,
            "Order"          INTEGER NOT NULL,
            Sets             INTEGER NOT NULL CHECK (Sets > 0),
            Reps             INTEGER NOT NULL CHECK (Reps > 0),
            Weight           REAL    CHECK (Weight IS NULL OR Weight >= 0),
            Rpe              REAL    CHECK (Rpe IS NULL OR Rpe BETWEEN 1 AND 10),
            RestSeconds      INTEGER CHECK (RestSeconds IS NULL OR RestSeconds >= 0),
            Notes            TEXT
        )
        """,
        """
        CREATE TABLE ScheduledWorkout (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            ProgramWorkoutId INTEGER NOT NULL REFERENCES ProgramWorkout(Id) ON DELETE CASCADE,
            Date             TEXT    NOT NULL,
            OriginalDate     TEXT,
            Status           INTEGER NOT NULL DEFAULT 0
        )
        """,
        """
        CREATE TABLE WorkoutSession (
            Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
            ScheduledWorkoutId INTEGER REFERENCES ScheduledWorkout(Id) ON DELETE SET NULL,
            Title              TEXT NOT NULL,
            StartedAt          TEXT NOT NULL,
            FinishedAt         TEXT,
            Comment            TEXT
        )
        """,
        """
        CREATE TABLE WorkoutExercise (
            Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
            WorkoutSessionId   INTEGER NOT NULL REFERENCES WorkoutSession(Id) ON DELETE CASCADE,
            ExerciseId         INTEGER NOT NULL REFERENCES Exercise(Id) ON DELETE RESTRICT,
            "Order"            INTEGER NOT NULL,
            PlannedSets        INTEGER,
            PlannedReps        INTEGER,
            PlannedWeight      REAL,
            PlannedRpe         REAL,
            PlannedRestSeconds INTEGER,
            IsSkipped          INTEGER NOT NULL DEFAULT 0,
            Notes              TEXT
        )
        """,
        """
        CREATE TABLE SetEntry (
            Id                INTEGER PRIMARY KEY AUTOINCREMENT,
            WorkoutExerciseId INTEGER NOT NULL REFERENCES WorkoutExercise(Id) ON DELETE CASCADE,
            SetNumber         INTEGER NOT NULL,
            Weight            REAL    NOT NULL CHECK (Weight >= 0),
            Reps              INTEGER NOT NULL CHECK (Reps > 0),
            Rpe               REAL    CHECK (Rpe IS NULL OR Rpe BETWEEN 1 AND 10),
            RestSeconds       INTEGER CHECK (RestSeconds IS NULL OR RestSeconds >= 0),
            Comment           TEXT,
            CompletedAt       TEXT    NOT NULL
        )
        """,
        """
        CREATE TABLE PersonalRecord (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            ExerciseId INTEGER NOT NULL REFERENCES Exercise(Id) ON DELETE RESTRICT,
            Type       INTEGER NOT NULL,
            Weight     REAL    NOT NULL CHECK (Weight > 0),
            AchievedAt TEXT    NOT NULL,
            SetEntryId INTEGER REFERENCES SetEntry(Id) ON DELETE SET NULL
        )
        """,

        // Индексы под внешние ключи и частые запросы (история, расписание, аналитика)
        "CREATE INDEX IX_ProgramWorkout_ProgramId ON ProgramWorkout(ProgramId)",
        "CREATE INDEX IX_ProgramExercise_ProgramWorkoutId ON ProgramExercise(ProgramWorkoutId)",
        "CREATE INDEX IX_ScheduledWorkout_Date ON ScheduledWorkout(Date)",
        "CREATE INDEX IX_ScheduledWorkout_ProgramWorkoutId ON ScheduledWorkout(ProgramWorkoutId)",
        "CREATE INDEX IX_WorkoutSession_StartedAt ON WorkoutSession(StartedAt)",
        "CREATE INDEX IX_WorkoutSession_ScheduledWorkoutId ON WorkoutSession(ScheduledWorkoutId)",
        "CREATE INDEX IX_WorkoutExercise_WorkoutSessionId ON WorkoutExercise(WorkoutSessionId)",
        "CREATE INDEX IX_WorkoutExercise_ExerciseId ON WorkoutExercise(ExerciseId)",
        "CREATE INDEX IX_SetEntry_WorkoutExerciseId ON SetEntry(WorkoutExerciseId)",
        "CREATE INDEX IX_PersonalRecord_ExerciseId_Type ON PersonalRecord(ExerciseId, Type)"
    ];
}