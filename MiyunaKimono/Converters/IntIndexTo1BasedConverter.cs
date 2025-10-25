using System;
using System.Globalization;
using System.Windows.Data;

namespace MiyunaKimono.Converters
{
    public class IntIndexTo1BasedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int i ? i + 1 : value;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => int.TryParse(value?.ToString(), out var n) ? Math.Max(0, n - 1) : 0;
    }
}
