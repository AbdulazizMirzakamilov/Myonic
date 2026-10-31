using System.Globalization;
using Myonic.Services;

namespace Myonic.Converters;

public class DayBgColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = value is true;
        bool isDark = Application.Current?.UserAppTheme != AppTheme.Light;

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
        bool isDark = Application.Current?.UserAppTheme != AppTheme.Light;

        if (isSelected)
        {
            return Color.FromArgb("#0F172A"); // Always clear high-contrast dark text on bright lime
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
        bool isDark = Application.Current?.UserAppTheme != AppTheme.Light;

        if (isSelected)
        {
            return Color.FromArgb("#1E293B"); // Clear dark slate on selected lime
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
        bool isDark = Application.Current?.UserAppTheme != AppTheme.Light;

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
        bool isDark = Application.Current?.UserAppTheme != AppTheme.Light;

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
