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
                // 1. ตรวจสอบ Path
                if (string.IsNullOrWhiteSpace(path))
                    return DefaultIcon();

                // 2. ตรวจสอบว่าเป็น Pack URI หรือไม่ (เช่น ไอคอนเริ่มต้น)
                if (path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateBitmapFromUri(new Uri(path, UriKind.Absolute));
                }

                // 3. ตรวจสอบว่าเป็นไฟล์บนดิสก์จริงหรือไม่
                string fullPath = Path.GetFullPath(path); // แปลงเป็น Path เต็ม
                if (!File.Exists(fullPath))
                {
                    Debug.WriteLine($"[ImageHelper] File not found: {fullPath}");
                    return DefaultIcon();
                }

                // 4. โหลดไฟล์โดยใช้ BitmapImage
                // นี่คือวิธีที่ถูกต้องในการโหลดและปล่อย Lock ทันที
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad; // ⬅️ โหลดเข้า Memory ทันที
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // ⬅️ ไม่ใช้ Cache เก่า
                bmp.UriSource = new Uri(fullPath, UriKind.Absolute); // ⬅️ ชี้ไปที่ไฟล์
                bmp.EndInit();
                bmp.Freeze(); // ⬅️ ทำให้ UI Thread ใช้งานได้
                return bmp;
            }
            catch (Exception ex)
            {
                // ถ้าไฟล์ถูก Lock, เป็น 0 byte, หรือเสียหาย
                Debug.WriteLine($"[ImageHelper] Failed to load {path}. Error: {ex.Message}");
                return DefaultIcon(); // คืนค่าไอคอนเริ่มต้น
            }
        }

        // Helper 1: สร้างจาก URI (ใช้สำหรับทั้ง pack และ file)
        private static BitmapImage CreateBitmapFromUri(Uri uri)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.UriSource = uri;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        // Helper 2: ไอคอนเริ่มต้น
        private static BitmapImage DefaultIcon()
        {
            // โหลดไอคอนเริ่มต้นโดยใช้ Helper เดียวกัน
            return CreateBitmapFromUri(new Uri("pack://application:,,,/Assets/ic_user.png", UriKind.Absolute));
        }
    }
}