// Converters/PathToImageConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MiyunaKimono.Converters
{
    /// <summary>
    /// แปลง path/URI ธรรมดา -> BitmapImage (กรณีรูป assets, URL, pack)
    /// </summary>
    public class PathToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return null;
            try
            {
                var uri = Uri.IsWellFormedUriString(s, UriKind.Absolute)
                    ? new Uri(s, UriKind.Absolute)
                    : new Uri(s, UriKind.RelativeOrAbsolute);

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bi.UriSource = uri;
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch { return null; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
