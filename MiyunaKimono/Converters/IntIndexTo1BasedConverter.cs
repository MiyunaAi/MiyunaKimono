namespace MiyunaKimono.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class IntIndexTo1BasedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int i ? (i + 1) : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int i ? i - 1 : Binding.DoNothing;
    }
}
