using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace MiyunaKimono.Helpers
{
    public static class ImageHelper
    {
        public static BitmapImage LoadBitmapNoCache(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return DefaultIcon();

                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var ms = new MemoryStream();
                fs.CopyTo(ms);
                ms.Position = 0;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ImageHelper] " + ex.Message);
                return DefaultIcon();
            }
        }

        private static BitmapImage DefaultIcon()
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.UriSource = new Uri("pack://application:,,,/Assets/ic_user.png");
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}
