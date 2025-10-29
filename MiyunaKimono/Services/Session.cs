// Services/Session.cs
using MiyunaKimono.Models;
using System.Windows.Media.Imaging;



namespace MiyunaKimono.Services
{


    public static class Session
    {
        // ใช้โมเดลของคุณตรง ๆ
        public static User CurrentUser { get; set; }
        // แจ้งทั่วแอปว่าโปรไฟล์เปลี่ยน (เช่น เปลี่ยนรูป/ชื่อ)
        public static event Action ProfileChanged;

        public static void RaiseProfileChanged() => ProfileChanged?.Invoke();

   
    }

}
