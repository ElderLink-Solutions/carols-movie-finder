using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MovieFinder
{
    public class StatusToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToUpper() switch
                {
                    "CONNECTED" => Brushes.LawnGreen,
                    "DISCONNECTED" => Brushes.Salmon,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
