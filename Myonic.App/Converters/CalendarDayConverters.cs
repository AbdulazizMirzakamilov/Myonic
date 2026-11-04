using System.Globalization;
using Myonic.Core.Calendar;
using Myonic.Services;

namespace Myonic.Converters;

public static class CalendarThemeHelper
{
    public static bool IsDark()
    {
        var themeService = IPlatformApplication.Current?.Services?.GetService<IThemeService>();
        if (themeService != null)
        {
            return themeService.IsDark;
        }

        var app = Application.Current;
        if (app?.UserAppTheme == AppTheme.Dark) return true;
        if (app?.UserAppTheme == AppTheme.Light) return false;
        return app?.RequestedTheme != AppTheme.Light;
    }
}

public class DayBgColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = value is true;
        bool isDark = CalendarThemeHelper.IsDark();

        if (isSelected)
        {
            return isDark ? Color.FromArgb("#CCFF00") : Color.FromArgb("#84CC16");
        }
        else
        {
            return isDark ? Color.FromArgb("#1F242D") : Color.FromArgb("#FFFFFF");
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DayTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = value is true;
        bool isDark = CalendarThemeHelper.IsDark();

        if (isSelected)
        {
            return Color.FromArgb("#0F172A"); // High-contrast dark text on bright lime
        }
        else
        {
            return isDark ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#0F172A");
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DayTextMutedColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = value is true;
        bool isDark = CalendarThemeHelper.IsDark();

        if (isSelected)
        {
            return Color.FromArgb("#1E293B");
        }
        else
        {
            return isDark ? Color.FromArgb("#94A3B8") : Color.FromArgb("#64748B");
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DayStrokeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = value is true;
        bool isDark = CalendarThemeHelper.IsDark();

        if (isSelected)
        {
            return isDark ? Color.FromArgb("#CCFF00") : Color.FromArgb("#84CC16");
        }
        else
        {
            return isDark ? Color.FromArgb("#2A313E") : Color.FromArgb("#CBD5E1");
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DayStrokeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 1.5 : 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DayCompletedIconColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = value is true;
        bool isDark = CalendarThemeHelper.IsDark();

        if (isSelected)
        {
            return Color.FromArgb("#0F172A");
        }
        else
        {
            return isDark ? Color.FromArgb("#10B981") : Color.FromArgb("#059669");
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class MonthDayOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 1.0 : 0.35;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class IsNotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return false;
        if (value is string s) return !string.IsNullOrWhiteSpace(s);
        if (value is System.Collections.ICollection collection) return collection.Count > 0;
        if (value is System.Collections.IEnumerable enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            return enumerator.MoveNext();
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CalendarWorkoutStatus status)
        {
            return status switch
            {
                CalendarWorkoutStatus.Completed => Color.FromArgb("#10B981"), // KineticEmerald
                CalendarWorkoutStatus.Planned => Color.FromArgb("#38BDF8"),   // KineticIceBlue
                _ => Color.FromArgb("#94A3B8")
            };
        }
        return Color.FromArgb("#94A3B8");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
