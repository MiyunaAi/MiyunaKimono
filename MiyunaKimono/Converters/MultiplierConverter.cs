// Converters/MultiplierConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace MiyunaKimono.Converters
{
    /// <summary>
    /// ใช้ใน XAML: Width="{Binding ActualWidth, Converter={StaticResource Mul}, ConverterParameter=0.364}"
    /// </summary>
    public class MultiplierConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0d;
            double x = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double m = 1.0;
            if (parameter != null)
                double.TryParse(parameter.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out m);
            return x * m;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
