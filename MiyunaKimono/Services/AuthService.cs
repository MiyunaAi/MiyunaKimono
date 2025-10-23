using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Services/AuthService.cs
using MiyunaKimono.Models;
using MySql.Data.MySqlClient;
using System;

namespace MiyunaKimono.Services
{
    // เก็บข้อมูลผู้ใช้แบบย่อ (ไว้ใช้ทั้งแอป)
    public class AppUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public bool IsAdmin { get; set; }
    }

    // Session ผู้ใช้ที่ล็อกอินอยู่ (optional)
    public static class Session
    {
        public static AppUser? CurrentUser { get; set; }
    }

    public class AuthService
    {
        /// <summary>
        /// ล็อกอิน: คืน true ถ้าสำเร็จ และเซ็ต Session.CurrentUser ให้ด้วย
        /// </summary>
        public bool Login(string username, string password)
        {
            var rec = GetUserRecordByUsername(username);
            if (rec == null) return false;

            // ตรวจรหัสผ่าน
            if (!PasswordHasher.Verify(password, rec.PasswordHash))
                return false;

            // ตีความ admin:
            // 1) ถ้ามีคอลัมน์ role และเป็น 'admin' => แอดมิน
            // 2) หากไม่มี role ให้ fallback ว่า username 'Ishihara' คือแอดมิน
            var isAdmin = (!string.IsNullOrWhiteSpace(rec.Role) && rec.Role.Trim().ToLower() == "admin")
                          || string.Equals(rec.Username, "Ishihara", System.StringComparison.OrdinalIgnoreCase);

            Session.CurrentUser = new AppUser
            {
                Id = rec.Id,
                Username = rec.Username,
                FirstName = rec.FirstName,
                LastName = rec.LastName,
                Email = rec.Email,
                IsAdmin = isAdmin
            };
            return true;
        }

        /// <summary>
        /// ตรวจว่าอีเมลนี้มีอยู่แล้วหรือไม่
        /// </summary>
        public bool ExistsEmail(string email)
        {
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand("SELECT 1 FROM users WHERE email=@e LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@e", email);
            var r = cmd.ExecuteScalar();
            return r != null;
        }

        /// <summary>
        /// ตรวจว่าเบอร์โทรนี้มีอยู่แล้วหรือไม่
        /// </summary>
        public bool ExistsPhone(string phone)
        {
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand("SELECT 1 FROM users WHERE phone=@p LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@p", phone);
            var r = cmd.ExecuteScalar();
            return r != null;
        }

        /// <summary>
        /// ตรวจว่า username นี้มีอยู่แล้วหรือไม่
        /// </summary>
        public bool ExistsUsername(string username)
        {
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand("SELECT 1 FROM users WHERE username=@u LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@u", username);
            var r = cmd.ExecuteScalar();
            return r != null;
        }

        /// <summary>
        /// ตรวจว่า plain password ตรงกับที่เก็บไว้ (อิงอีเมล)
        /// </summary>
        public bool IsSamePassword(string email, string plain)
        {
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand("SELECT password_hash FROM users WHERE email=@e LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@e", email);

            var hash = cmd.ExecuteScalar() as string;
            if (hash == null) return false;
            return PasswordHasher.Verify(plain, hash);
        }

        /// <summary>
        /// รีเซ็ตรหัสผ่านใหม่ (อิงอีเมล)
        /// </summary>
        public bool ResetPassword(string email, string newPlain)
        {
            var newHash = PasswordHasher.Hash(newPlain);
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand("UPDATE users SET password_hash=@h WHERE email=@e", conn);
            cmd.Parameters.AddWithValue("@h", newHash);
            cmd.Parameters.AddWithValue("@e", email);
            return cmd.ExecuteNonQuery() == 1;
        }

        /// <summary>
        /// สมัครสมาชิกใหม่ (กันซ้ำอีกรอบก่อน insert)
        /// </summary>
        public bool Register(string first, string last, string email, string phone, string username, string password)
        {
            if (ExistsEmail(email) || ExistsPhone(phone) || ExistsUsername(username))
                return false;

            var hash = PasswordHasher.Hash(password);

            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand(
                @"INSERT INTO users(first_name,last_name,email,phone,username,password_hash,created_at)
                  VALUES(@f,@l,@e,@p,@u,@h,NOW())", conn);

            cmd.Parameters.AddWithValue("@f", first);
            cmd.Parameters.AddWithValue("@l", last);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", phone);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@h", hash);

            return cmd.ExecuteNonQuery() == 1;
        }

        // ---------------------- Helpers ----------------------

        private sealed class UserRecord
        {
            public int Id { get; set; }
            public string Username { get; set; } = "";
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string PasswordHash { get; set; } = "";
            public string? Role { get; set; }   // อาจไม่มีในตารางก็ได้
        }

        private UserRecord? GetUserRecordByUsername(string username)
        {
            // พยายามดึง role; ถ้าไม่มีคอลัมน์ role ใน DB จริง ๆ
            // คำสั่งนี้จะ error — ในกรณีนั้นให้แก้เป็น SELECT เฉพาะคอลัมน์ที่มี
            // หรือสร้างคอลัมน์ role (VARCHAR(20)) เพิ่มในตาราง users
            const string sql = @"
                SELECT id, username, first_name, last_name, email, password_hash,
                       /* ถ้ามีคอลัมน์ role ให้ select มาด้วย; ถ้าไม่มี ให้คอมเมนต์บรรทัดนี้ */
                       role
                FROM users
                WHERE username=@u
                LIMIT 1";

            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);

            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;

            var rec = new UserRecord
            {
                Id = rd.GetInt32("id"),
                Username = rd.GetString("username"),
                FirstName = rd["first_name"] as string,
                LastName = rd["last_name"] as string,
                Email = rd["email"] as string,
                PasswordHash = rd.GetString("password_hash"),
            };

            // อ่าน role ถ้ามี
            var roleOrdinal = SafeOrdinal(rd, "role");
            if (roleOrdinal >= 0 && !rd.IsDBNull(roleOrdinal))
                rec.Role = rd.GetString(roleOrdinal);

            return rec;
        }

        private static int SafeOrdinal(MySqlDataReader rd, string col)
        {
            try { return rd.GetOrdinal(col); }
            catch { return -1; }
        }
    }
}