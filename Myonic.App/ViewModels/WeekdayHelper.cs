namespace Myonic.ViewModels;

internal static class WeekdayHelper
{
    public static readonly IReadOnlyList<string> Options =
        ["Не задан", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"];

    public static int ToIndex(DayOfWeek? day) => day switch
    {
        null => 0,
        DayOfWeek.Sunday => 7,
        var d => (int)d
    };

    public static DayOfWeek? FromIndex(int index) => index switch
    {
        <= 0 => null,
        7 => DayOfWeek.Sunday,
        _ => (DayOfWeek)index
    };

    public static string ToText(DayOfWeek? day) => Options[ToIndex(day)];
}