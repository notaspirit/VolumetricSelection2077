using Avalonia.Data.Converters;
using System;
using System.Globalization;

public class IntToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int intValue ? intValue.ToString() : string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return int.TryParse(value?.ToString(), out int result) ? result : 0; // Default to 0 if invalid input
    }
}
