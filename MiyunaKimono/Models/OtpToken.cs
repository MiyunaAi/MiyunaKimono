using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/OtpToken.cs
using System;

namespace MiyunaKimono.Models
{
    public class OtpToken
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Otp_Code { get; set; } = "";
        public DateTime Expires_At { get; set; }
        public DateTime Last_Sent_At { get; set; }
        public bool Consumed { get; set; }
    }
}

