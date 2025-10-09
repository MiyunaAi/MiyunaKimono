using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/User.cs
namespace MiyunaKimono.Models
{
    public class User
    {
        public int Id { get; set; }
        public string First_Name { get; set; } = "";
        public string Last_Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password_Hash { get; set; } = "";
    }
}
