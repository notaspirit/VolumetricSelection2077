using Avalonia.Data.Converters;
using Avalonia.Media;
using Serilog.Events;
using System;
using System.Globalization;

namespace VolumetricSelection2077
{
    public class LogLevelColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is LogEventLevel level)
            {
                return level switch
                {
                    LogEventLevel.Verbose => new SolidColorBrush(Colors.Gray),
                    LogEventLevel.Debug => new SolidColorBrush(Colors.LightGray),
                    LogEventLevel.Information => new SolidColorBrush(Colors.White),
                    LogEventLevel.Warning => new SolidColorBrush(Colors.Yellow),
                    LogEventLevel.Error => new SolidColorBrush(Colors.Red),
                    LogEventLevel.Fatal => new SolidColorBrush(Colors.DarkRed),
                    _ => new SolidColorBrush(Colors.White)
                };
            }
            return new SolidColorBrush(Colors.White);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}