using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Services/PasswordHasher.cs
using BCrypt.Net;

namespace MiyunaKimono.Services
{
    public static class PasswordHasher
    {
        public static string Hash(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

        public static bool Verify(string password, string hash) =>
            BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

