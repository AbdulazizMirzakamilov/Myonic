using Myonic.Core.Models;

namespace Myonic.Data.Migrations;

internal static class SeedExercises
{
    private const ExerciseType C = ExerciseType.Compound;
    private const ExerciseType I = ExerciseType.Isolation;

    // Порядок важен: Items должен быть объявлен раньше Statements
    private static readonly (string Name, ExerciseType Type)[] Items =
    [
        // Базовые
        ("Жим лёжа", C),
        ("Жим лёжа на наклонной скамье", C),
        ("Жим гантелей лёжа", C),
        ("Жим штанги стоя", C),
        ("Приседания со штангой", C),
        ("Фронтальные приседания", C),
        ("Становая тяга", C),
        ("Румынская тяга", C),
        ("Подтягивания", C),
        ("Тяга штанги в наклоне", C),
        ("Тяга верхнего блока", C),
        ("Тяга нижнего блока", C),
        ("Отжимания на брусьях", C),
        ("Жим ногами", C),
        ("Выпады с гантелями", C),

        // Изолирующие
        ("Разведение гантелей лёжа", I),
        ("Сведение рук в кроссовере", I),
        ("Подъём штанги на бицепс", I),
        ("Молотковые сгибания", I),
        ("Разгибание рук на блоке", I),
        ("Французский жим", I),
        ("Махи гантелей в стороны", I),
        ("Разведение гантелей в наклоне", I),
        ("Разгибание ног в тренажёре", I),
        ("Сгибание ног лёжа", I),
        ("Подъём на носки стоя", I)
    ];

    public static readonly string[] Statements = [BuildInsert()];

    private static string BuildInsert()
    {
        var rows = Items.Select(i =>
            $"('{i.Name.Replace("'", "''")}', {(int)i.Type}, 0, 0)");

        return "INSERT INTO Exercise (Name, Type, IsCustom, IsArchived) VALUES "
               + string.Join(", ", rows);
    }
}