using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.Globalization;
using System.Windows.Data;



namespace MiyunaKimono
{
    public class IntIndexTo1BasedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int i ? (i + 1).ToString() : "1";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}