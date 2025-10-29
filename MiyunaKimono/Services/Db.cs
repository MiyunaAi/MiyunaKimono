using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Services/Db.cs
using MySql.Data.MySqlClient;

namespace MiyunaKimono.Services
{

    public static class Db
    {
        public static MySqlConnection GetOpenConnection() => GetConn();
        // แก้ค่าให้ตรงกับ XAMPP ของคุณ
        private const string ConnStr =
            "Server=localhost;Port=3306;Database=miyuna_kimono;Uid=root;Pwd=;SslMode=None;";

        public static MySqlConnection GetConn()
        {
            var conn = new MySqlConnection(ConnStr);
            conn.Open();
            return conn;
        }
    }
}

