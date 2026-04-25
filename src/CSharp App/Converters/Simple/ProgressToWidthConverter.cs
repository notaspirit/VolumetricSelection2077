using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace VolumetricSelection2077.Converters.Simple;

public sealed class ProgressToWidthConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 4)
            return 0d;

        var value = ToDouble(values[0]);
        var min = ToDouble(values[1]);
        var max = ToDouble(values[2]);
        var width = ToDouble(values[3]);

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
            return 0d;

        var range = max - min;
        if (range <= 0)
            return 0d;

        var t = (value - min) / range;
        t = Math.Clamp(t, 0d, 1d);

        return width * t;
    }

    private static double ToDouble(object? o)
        => o switch
        {
            null => 0d,
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            decimal m => (double)m,
            _ when double.TryParse(o.ToString(), out var parsed) => parsed,
            _ => 0d
        };
}