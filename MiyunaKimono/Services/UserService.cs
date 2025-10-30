using MiyunaKimono.Services;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Threading.Tasks;

public sealed class UserService
{
    public static UserService Instance { get; } = new();

    public async Task<string> UpdateProfileAsync(
    int userId, string first, string last, string email, string phone, byte[] avatarBytes)
    {
        string avatarPath = null;

        if (avatarBytes != null && avatarBytes.Length > 0)
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MiyunaKimono", "avatars");
            Directory.CreateDirectory(root);

            var fileName = $"user_{userId}_{DateTime.UtcNow.Ticks}.png"; // ชื่อใหม่ทุกครั้ง
            avatarPath = Path.Combine(root, fileName);
            await File.WriteAllBytesAsync(avatarPath, avatarBytes);

            // เก็บบ้านให้เรียบร้อย ลบไฟล์เก่าของ user นี้
            foreach (var f in Directory.GetFiles(root, $"user_{userId}_*.png"))
                if (!f.Equals(avatarPath, StringComparison.OrdinalIgnoreCase))
                    try { File.Delete(f); } catch { /* ignore */ }
        }

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

        if (avatarPath != null)
        {
            Session.CurrentUser.AvatarPath = avatarPath;
            Session.UpdateAvatarPath(avatarPath);   // 🎯 แจ้งทุกหน้าว่า path ใหม่แล้ว
        }
        return avatarPath;

        // sync session
        if (Session.CurrentUser != null)
        {
            Session.CurrentUser.First_Name = first;
            Session.CurrentUser.Last_Name = last;
            Session.CurrentUser.Email = email;
            Session.CurrentUser.Phone = phone;
            if (avatarPath != null) Session.CurrentUser.AvatarPath = avatarPath;
        }
        return avatarPath;
    }


}
