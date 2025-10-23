// Converters/DbImageConverter.cs
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MiyunaKimono.Converters
{
    /// <summary>
    /// แปลง path จาก DB/ไฟล์/pack URI -> BitmapImage
    /// รองรับทั้งเส้นทางเต็ม, relative, หรือ pack://
    /// </summary>
    public class DbImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path)) return null;

            try
            {
                // รองรับ pack://
                if (path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                    return new BitmapImage(new Uri(path, UriKind.Absolute));

                // ถ้าเป็น relative ให้ลองหาตามโฟลเดอร์ทำงาน
                string full = path;
                if (!Path.IsPathRooted(full))
                    full = Path.GetFullPath(path);

                if (!File.Exists(full)) return null;

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(full, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
