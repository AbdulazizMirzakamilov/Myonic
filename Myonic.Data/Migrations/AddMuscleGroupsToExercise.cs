namespace Myonic.Data.Migrations;

public static class AddMuscleGroupsToExercise
{
    public static readonly IReadOnlyList<string> Statements =
    [
        "ALTER TABLE Exercise ADD COLUMN MuscleGroup INTEGER NOT NULL DEFAULT 0;",
        "ALTER TABLE Exercise ADD COLUMN SecondaryMuscleGroup INTEGER;",
        "CREATE INDEX IF NOT EXISTS IX_Exercise_MuscleGroup ON Exercise(MuscleGroup);",

        // Базовые упражнения
        "UPDATE Exercise SET MuscleGroup = 1, SecondaryMuscleGroup = 5 WHERE Name = 'Жим лёжа';",
        "UPDATE Exercise SET MuscleGroup = 1, SecondaryMuscleGroup = 3 WHERE Name = 'Жим лёжа на наклонной скамье';",
        "UPDATE Exercise SET MuscleGroup = 1, SecondaryMuscleGroup = 5 WHERE Name = 'Жим гантелей лёжа';",
        "UPDATE Exercise SET MuscleGroup = 3, SecondaryMuscleGroup = 5 WHERE Name = 'Жим штанги стоя';",
        "UPDATE Exercise SET MuscleGroup = 7, SecondaryMuscleGroup = 9 WHERE Name = 'Приседания со штангой';",
        "UPDATE Exercise SET MuscleGroup = 7, SecondaryMuscleGroup = 9 WHERE Name = 'Фронтальные приседания';",
        "UPDATE Exercise SET MuscleGroup = 2, SecondaryMuscleGroup = 8 WHERE Name = 'Становая тяга';",
        "UPDATE Exercise SET MuscleGroup = 8, SecondaryMuscleGroup = 9 WHERE Name = 'Румынская тяга';",
        "UPDATE Exercise SET MuscleGroup = 2, SecondaryMuscleGroup = 4 WHERE Name = 'Подтягивания';",
        "UPDATE Exercise SET MuscleGroup = 2, SecondaryMuscleGroup = 4 WHERE Name = 'Тяга штанги в наклоне';",
        "UPDATE Exercise SET MuscleGroup = 2, SecondaryMuscleGroup = 4 WHERE Name = 'Тяга верхнего блока';",
        "UPDATE Exercise SET MuscleGroup = 2, SecondaryMuscleGroup = 4 WHERE Name = 'Тяга нижнего блока';",
        "UPDATE Exercise SET MuscleGroup = 1, SecondaryMuscleGroup = 5 WHERE Name = 'Отжимания на брусьях';",
        "UPDATE Exercise SET MuscleGroup = 7, SecondaryMuscleGroup = 9 WHERE Name = 'Жим ногами';",
        "UPDATE Exercise SET MuscleGroup = 7, SecondaryMuscleGroup = 9 WHERE Name = 'Выпады с гантелями';",

        // Изолирующие упражнения
        "UPDATE Exercise SET MuscleGroup = 1, SecondaryMuscleGroup = NULL WHERE Name = 'Разведение гантелей лёжа';",
        "UPDATE Exercise SET MuscleGroup = 1, SecondaryMuscleGroup = NULL WHERE Name = 'Сведение рук в кроссовере';",
        "UPDATE Exercise SET MuscleGroup = 4, SecondaryMuscleGroup = NULL WHERE Name = 'Подъём штанги на бицепс';",
        "UPDATE Exercise SET MuscleGroup = 4, SecondaryMuscleGroup = 6 WHERE Name = 'Молотковые сгибания';",
        "UPDATE Exercise SET MuscleGroup = 5, SecondaryMuscleGroup = NULL WHERE Name = 'Разгибание рук на блоке';",
        "UPDATE Exercise SET MuscleGroup = 5, SecondaryMuscleGroup = NULL WHERE Name = 'Французский жим';",
        "UPDATE Exercise SET MuscleGroup = 3, SecondaryMuscleGroup = NULL WHERE Name = 'Махи гантелей в стороны';",
        "UPDATE Exercise SET MuscleGroup = 3, SecondaryMuscleGroup = NULL WHERE Name = 'Разведение гантелей в наклоне';",
        "UPDATE Exercise SET MuscleGroup = 7, SecondaryMuscleGroup = NULL WHERE Name = 'Разгибание ног в тренажёре';",
        "UPDATE Exercise SET MuscleGroup = 8, SecondaryMuscleGroup = NULL WHERE Name = 'Сгибание ног лёжа';",
        "UPDATE Exercise SET MuscleGroup = 10, SecondaryMuscleGroup = NULL WHERE Name = 'Подъём на носки стоя';"
    ];
}
