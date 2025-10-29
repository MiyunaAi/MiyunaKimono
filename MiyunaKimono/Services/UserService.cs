// Services/UserService.cs
using MiyunaKimono.Services;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Threading.Tasks;

public sealed class UserService
{
    public static UserService Instance { get; } = new();

    public async Task<string> UpdateProfileAsync(  // ← คืน string
        int userId,
        string first, string last, string email, string phone, byte[] avatarBytes)
    {
        string avatarPath = null;

        if (avatarBytes != null && avatarBytes.Length > 0)
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MiyunaKimono", "avatars");
            Directory.CreateDirectory(root);

            avatarPath = Path.Combine(root, $"user_{userId}.png");
            await File.WriteAllBytesAsync(avatarPath, avatarBytes);
        }

        using var conn = Db.GetConn();            // ← ใช้ GetConn()
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE users
               SET first_name=@f, last_name=@l, email=@e, phone=@p
                   {0}
             WHERE id=@id";
        var setAvatar = avatarPath != null ? ", avatar_path=@a" : "";
        cmd.CommandText = string.Format(cmd.CommandText, setAvatar);

        cmd.Parameters.AddWithValue("@f", first);
        cmd.Parameters.AddWithValue("@l", last);
        cmd.Parameters.AddWithValue("@e", email);
        cmd.Parameters.AddWithValue("@p", phone);
        cmd.Parameters.AddWithValue("@id", userId);
        if (avatarPath != null) cmd.Parameters.AddWithValue("@a", avatarPath);

        await cmd.ExecuteNonQueryAsync();

        // sync Session (ทันที)
        if (Session.CurrentUser != null)
        {
            Session.CurrentUser.First_Name = first;
            Session.CurrentUser.Last_Name = last;
            Session.CurrentUser.Email = email;
            Session.CurrentUser.Phone = phone;
            if (avatarPath != null) Session.CurrentUser.AvatarPath = avatarPath;
            Session.RaiseProfileChanged();
        }

        return avatarPath;   // ← คืน path (อาจเป็น null)
    }
}
