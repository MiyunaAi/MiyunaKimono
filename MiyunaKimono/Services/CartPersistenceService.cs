using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiyunaKimono.Services
{
    public class CartPersistenceService
    {
        public static CartPersistenceService Instance { get; } = new();

        public void Save(int userId, IList<CartLine> lines)
        {
            using var conn = Db.GetConn();
            using var tx = conn.BeginTransaction();
            try
            {
                // เคลียร์ของเดิม
                using (var del = new MySqlCommand("DELETE FROM user_carts WHERE user_id=@u;", conn, tx))
                { del.Parameters.AddWithValue("@u", userId); del.ExecuteNonQuery(); }

                // แทรกใหม่ทั้งหมด
                foreach (var l in lines)
                {
                    using var ins = new MySqlCommand(
                        "INSERT INTO user_carts (user_id, product_id, quantity) VALUES (@u,@p,@q);", conn, tx);
                    ins.Parameters.AddWithValue("@u", userId);
                    ins.Parameters.AddWithValue("@p", l.Product.Id);
                    ins.Parameters.AddWithValue("@q", l.Quantity);
                    ins.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public void Load(int userId)
        {
            // ล้างในหน่วยความจำก่อน
            CartService.Instance.Clear();

            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand("SELECT product_id, quantity FROM user_carts WHERE user_id=@u;", conn);
            cmd.Parameters.AddWithValue("@u", userId);
            using var rd = cmd.ExecuteReader();
            var temp = new List<(int pid, int qty)>();
            while (rd.Read())
                temp.Add((Convert.ToInt32(rd["product_id"]), Convert.ToInt32(rd["quantity"])));

            rd.Close();

            var productSvc = new ProductService();
            foreach (var (pid, qty) in temp)
            {
                var p = productSvc.GetById(pid);
                if (p != null) CartService.Instance.Add(p, Math.Max(1, qty));
            }
        }
    }
}
