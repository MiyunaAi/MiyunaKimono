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


    public class AuthService
    {
        /// <summary>
        /// ล็อกอิน: คืน true ถ้าสำเร็จ และเซ็ต Session.CurrentUser ให้ด้วย
        /// </summary>
        // Services/AuthService.cs
        // Services/AuthService.cs
        public async Task<bool> LoginAsync(string username, string password)
        {
            using var conn = Db.GetOpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT id, username, first_name, last_name, email, phone, avatar_path, password_hash
        FROM users
        WHERE username=@u
        LIMIT 1";
            cmd.Parameters.AddWithValue("@u", username);

            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
                return false; // ไม่พบ username

            // อ่าน hash จาก DB
            var dbHash = rd.IsDBNull(7) ? null : rd.GetString(7);
            if (string.IsNullOrEmpty(dbHash) || !PasswordHasher.Verify(password, dbHash))
                return false; // รหัสผ่านไม่ถูกต้อง

            // ถ้าถูกต้อง ค่อยแมป user
            var user = new MiyunaKimono.Models.User
            {
                Id = rd.GetInt32(0),
                Username = rd.GetString(1),
                First_Name = rd.IsDBNull(2) ? "" : rd.GetString(2),
                Last_Name = rd.IsDBNull(3) ? "" : rd.GetString(3),
                Email = rd.IsDBNull(4) ? "" : rd.GetString(4),
                Phone = rd.IsDBNull(5) ? "" : rd.GetString(5),
                AvatarPath = rd.IsDBNull(6) ? null : rd.GetString(6)
            };

            // ตั้ง session + กระจาย event
            Session.CurrentUser = user;
            SetCurrentUserId(user.Id);
            Session.RaiseProfileChanged();
            return true;
        }



        public int GetUserIdByUsername(string username)
        {
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand("SELECT id FROM users WHERE username=@u LIMIT 1;", conn);
            cmd.Parameters.AddWithValue("@u", username);
            var obj = cmd.ExecuteScalar();
            return (obj == null || obj == DBNull.Value) ? 0 : Convert.ToInt32(obj);
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
            public string? Phone { get; set; }   // ★ เพิ่ม
            public string PasswordHash { get; set; } = "";
            public string? Role { get; set; }
        }


        private UserRecord? GetUserRecordByUsername(string username)
        {
            // ★ ดึง phone มาด้วย
            const string sql = @"
                SELECT id, username, first_name, last_name, email, phone, password_hash,
                       /* ถ้ามีคอลัมน์ role อยู่ ให้ select มาด้วย; ถ้าไม่มีคอมเมนต์บรรทัดนี้ */
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
                Phone = rd["phone"] as string,   // ★ อ่าน phone
                PasswordHash = rd.GetString("password_hash"),
            };

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
        // เก็บ user ที่ล็อกอินปัจจุบันแบบ static
        private static int _currentUserId;
        public static int CurrentUserId => _currentUserId;
        public static int CurrentUserIdSafe() => _currentUserId; // ถ้ายังไม่ล็อกอินจะเป็น 0

        // เรียกเมธอดนี้หลัง Login สำเร็จเพื่อเซ็ตค่า user id ปัจจุบัน
        public static void SetCurrentUserId(int userId)
        {
            _currentUserId = userId;
        }




    }
}