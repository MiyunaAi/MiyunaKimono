using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MiyunaKimono.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        // false -> Collapsed (ค่าเริ่มต้น), ตั้งเป็น false ถ้าอยาก Hidden
        public bool CollapseWhenFalse { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool bb && bb;

            // ใส่ parameter="invert" เพื่อกลับค่าได้
            if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase))
                b = !b;

            if (b) return Visibility.Visible;
            return CollapseWhenFalse ? Visibility.Collapsed : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
