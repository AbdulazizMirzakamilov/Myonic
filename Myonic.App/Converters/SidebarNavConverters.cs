using System.Globalization;
using Microsoft.Maui.Controls;

namespace Myonic.Converters;

public class ActiveNavBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = value is true;
        if (isActive)
        {
            if (Application.Current?.Resources.TryGetValue("KineticVolt", out var volt) == true)
            {
                return volt;
            }
            return Color.FromArgb("#22C55E");
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ActiveNavTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = value is true;
        if (isActive)
        {
            if (Application.Current?.Resources.TryGetValue("KineticVoltText", out var voltText) == true)
            {
                return voltText;
            }
            return Color.FromArgb("#0F1115");
        }

        if (Application.Current?.Resources.TryGetValue("KineticTextSecondary", out var sec) == true)
        {
            return sec;
        }
        return Color.FromArgb("#94A3B8");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ActiveNavStrokeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = value is true;
        if (isActive)
        {
            if (Application.Current?.Resources.TryGetValue("KineticVolt", out var volt) == true)
            {
                return volt;
            }
            return Color.FromArgb("#22C55E");
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ActiveNavFontConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? FontAttributes.Bold : FontAttributes.None;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
