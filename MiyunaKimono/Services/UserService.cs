using MiyunaKimono.Services;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Threading.Tasks;

using MiyunaKimono.Services;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Threading.Tasks;

// ... (using statements) ...

public sealed class UserService
{
    public static UserService Instance { get; } = new();

    public async Task<int> GetTotalCustomerCountAsync()
    {
        using var conn = Db.GetConn();
        // (เราจะนับเฉพาะ role 'user', ไม่นับ 'admin')
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE role = 'user'", conn);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<string> UpdateProfileAsync(
    int userId, string first, string last, string email, string phone, byte[] avatarBytes)
    {
        string avatarPath = null;

        if (avatarBytes != null && avatarBytes.Length > 0)
        {
            // ... (โค้ดบันทึกไฟล์รูปภาพเหมือนเดิม) ...
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MiyunaKimono", "avatars");
            Directory.CreateDirectory(root);

            var fileName = $"user_{userId}_{DateTime.UtcNow.Ticks}.png";
            avatarPath = Path.Combine(root, fileName);
            await File.WriteAllBytesAsync(avatarPath, avatarBytes);

            foreach (var f in Directory.GetFiles(root, $"user_{userId}_*.png"))
                if (!f.Equals(avatarPath, StringComparison.OrdinalIgnoreCase))
                    try { File.Delete(f); } catch { /* ignore */ }
        }

        // ... (โค้ดอัปเดตฐานข้อมูล (DB) เหมือนเดิม) ...
        using var conn = Db.GetOpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
UPDATE users
   SET first_name=@f, last_name=@l, email=@e, phone=@p
       {(avatarPath != null ? ", avatar_path=@a" : "")}
 WHERE id=@id";
        cmd.Parameters.AddWithValue("@f", first);
        cmd.Parameters.AddWithValue("@l", last);
        cmd.Parameters.AddWithValue("@e", email);
        cmd.Parameters.AddWithValue("@p", phone);
        cmd.Parameters.AddWithValue("@id", userId);
        if (avatarPath != null) cmd.Parameters.AddWithValue("@a", avatarPath);
        await cmd.ExecuteNonQueryAsync();

        // --- 🔽 START FIX (สำคัญ) 🔽 ---

        // 1. อัปเดตข้อมูลใน Session.CurrentUser ให้ตรงกับที่บันทึกลง DB
        if (Session.CurrentUser != null)
        {
            Session.CurrentUser.First_Name = first;
            Session.CurrentUser.Last_Name = last;
            Session.CurrentUser.Email = email;
            Session.CurrentUser.Phone = phone;
            if (avatarPath != null)
            {
                Session.CurrentUser.AvatarPath = avatarPath;
            }
        }

        // 2. แจ้งเตือนทุกส่วนของแอป (รวมถึง UserInfoView) ว่าข้อมูลเปลี่ยนแล้ว
        Session.RaiseProfileChanged();

        // --- 🔼 END FIX 🔼 ---

        return avatarPath; // คืนค่า path รูปใหม่ (หรือ null)
    }
}
