// Services/OtpService.cs
using MiyunaKimono.Models;
using MySql.Data.MySqlClient;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
// Services/EmailService.cs
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;


namespace MiyunaKimono.Services
{
    public class OtpService
    {
        private readonly EmailService _email;
        public OtpService() : this(new EmailService()) { }
        public OtpService(EmailService email) => _email = email;

        // สุ่มรหัส 6 หลักแบบปลอดภัย
        private static string GenerateOtp()
        {
            // 000000–999999 (เติม 0 หน้าให้ครบ 6)
            var bytes = RandomNumberGenerator.GetBytes(4);
            int value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
            int code = value % 1_000_000;
            return code.ToString("D6");
        }

        public async Task<(bool ok, string err)> SendOtpAsync(string email)
        {
            var now = DateTime.UtcNow;

            using (var conn = Db.GetConn())
            {
                // อ่านอันล่าสุด
                using (var cmdSel = new MySqlCommand(
                    "SELECT last_sent_at FROM otp_tokens WHERE email=@e ORDER BY id DESC LIMIT 1", conn))
                {
                    cmdSel.Parameters.AddWithValue("@e", email);
                    var lastObj = cmdSel.ExecuteScalar();

                    if (lastObj != null &&
                        (now - Convert.ToDateTime(lastObj)).TotalSeconds < 60)
                    {
                        return (false, "Please wait 60 seconds before resending OTP.");
                    }
                }

                var otp = GenerateOtp();
                var expires = now.AddMinutes(10);

                using (var cmdIns = new MySqlCommand(@"
                    INSERT INTO otp_tokens(email,otp_code,expires_at,last_sent_at,consumed)
                    VALUES(@e,@o,@x,@s,0)", conn))
                {
                    cmdIns.Parameters.AddWithValue("@e", email);
                    cmdIns.Parameters.AddWithValue("@o", otp);
                    cmdIns.Parameters.AddWithValue("@x", expires);
                    cmdIns.Parameters.AddWithValue("@s", now);
                    cmdIns.ExecuteNonQuery();
                }

                string body = $@"
                    <p>รหัส OTP สำหรับรีเซ็ตรหัสผ่าน MiyunaKimono คือ:</p>
                    <h2 style=""letter-spacing:2px"">{otp}</h2>
                    <p>รหัสจะหมดอายุใน 10 นาที</p>";

                await _email.SendAsync(email, "MiyunaKimono Password Reset OTP", body);
                return (true, "");
            }
        }

        // wrapper sync (ถ้าอยากใช้แบบไม่ async ในบางที่)
        public (bool ok, string err) SendOtp(string email)
            => SendOtpAsync(email).GetAwaiter().GetResult();

        public bool VerifyOtpAndConsume(string email, string otp)
        {
            using (var conn = Db.GetConn())
            {
                int? id = null;
                DateTime? exp = null;
                bool consumed = true;

                using (var cmd = new MySqlCommand(@"
                    SELECT id, expires_at, consumed
                    FROM otp_tokens
                    WHERE email=@e AND otp_code=@o
                    ORDER BY id DESC LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.Parameters.AddWithValue("@o", otp);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            id = rd.GetInt32("id");
                            exp = rd.GetDateTime("expires_at");
                            consumed = rd.GetBoolean("consumed");
                        }
                    }
                }

                if (id == null || consumed || DateTime.UtcNow > exp) return false;

                using (var up = new MySqlCommand("UPDATE otp_tokens SET consumed=1 WHERE id=@id", conn))
                {
                    up.Parameters.AddWithValue("@id", id.Value);
                    up.ExecuteNonQuery();
                }
                return true;
            }
        }
    }
}


