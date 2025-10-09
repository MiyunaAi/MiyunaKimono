using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Services/EmailService.cs (ใช้ MailKit)
using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

namespace MiyunaKimono.Services
{
    public class EmailService
    {
        // ตั้งค่าบัญชีส่งอีเมล (เช่น Gmail: เปิด App Password)
        private readonly string _host = "smtp.gmail.com";
        private readonly int _port = 587;
        private readonly string _user = "miyuna5247@gmail.com";
        private readonly string _pass = "udvg venr ftpi avov";
        private readonly string _fromName = "MiyunaKimono";

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _user));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_host, _port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_user, _pass);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}

