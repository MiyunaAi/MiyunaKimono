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
    public class AuthService
    {
        // สมมติว่ามีคลาส Db.GetConn() เปิดคอนเนคชัน MySQL ใช้งานอยู่แล้ว

        public bool Login(string username, string password)
        {
            using (var conn = Db.GetConn())
            using (var cmd = new MySqlCommand("SELECT password_hash FROM users WHERE username=@u LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@u", username);
                var hashObj = cmd.ExecuteScalar();
                if (hashObj == null) return false;
                return PasswordHasher.Verify(password, hashObj.ToString());
            }
        }

        public bool ExistsEmail(string email)
        {
            using (var conn = Db.GetConn())
            using (var cmd = new MySqlCommand("SELECT 1 FROM users WHERE email=@e LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@e", email);
                var r = cmd.ExecuteScalar();
                return r != null;
            }
        }

        public bool IsSamePassword(string email, string plain)
        {
            using (var conn = Db.GetConn())
            using (var cmd = new MySqlCommand("SELECT password_hash FROM users WHERE email=@e LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@e", email);
                var hash = cmd.ExecuteScalar()?.ToString();
                if (hash == null) return false;
                return PasswordHasher.Verify(plain, hash);
            }
        }

        public bool ResetPassword(string email, string newPlain)
        {
            var newHash = PasswordHasher.Hash(newPlain);
            using (var conn = Db.GetConn())
            using (var cmd = new MySqlCommand("UPDATE users SET password_hash=@h WHERE email=@e", conn))
            {
                cmd.Parameters.AddWithValue("@h", newHash);
                cmd.Parameters.AddWithValue("@e", email);
                return cmd.ExecuteNonQuery() == 1;
            }
        }
        public bool ExistsPhone(string phone)
        {
            using (var conn = Db.GetConn())
            using (var cmd = new MySqlCommand("SELECT 1 FROM users WHERE phone=@p LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@p", phone);
                var r = cmd.ExecuteScalar();
                return r != null;
            }
        }

        public bool ExistsUsername(string username)
        {
            using (var conn = Db.GetConn())
            using (var cmd = new MySqlCommand("SELECT 1 FROM users WHERE username=@u LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@u", username);
                var r = cmd.ExecuteScalar();
                return r != null;
            }
        }

        /// <summary>
        /// สมัครสมาชิกใหม่ คืน true เมื่อสำเร็จ
        /// </summary>
        public bool Register(string first, string last, string email, string phone, string username, string password)
        {
            // ตรวจซ้ำอีกรอบเพื่อความปลอดภัย (ป้องกัน race)
            if (ExistsEmail(email) || ExistsPhone(phone) || ExistsUsername(username))
                return false;

            var hash = PasswordHasher.Hash(password);
            using (var conn = Db.GetConn())
            using (var cmd = new MySqlCommand(
                @"INSERT INTO users(first_name,last_name,email,phone,username,password_hash,created_at)
                  VALUES(@f,@l,@e,@p,@u,@h,NOW())", conn))
            {
                cmd.Parameters.AddWithValue("@f", first);
                cmd.Parameters.AddWithValue("@l", last);
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@p", phone);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@h", hash);

                var rows = cmd.ExecuteNonQuery();
                return rows == 1;
            }
        }
    }
}