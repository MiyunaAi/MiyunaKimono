using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiyunaKimono.Services
{
    public class OrderService
    {
        public static OrderService Instance { get; } = new();

        public void CreateOrder(int userId, string address, IList<CartLine> lines)
        {
            using var conn = Db.GetConn();
            using var tx = conn.BeginTransaction();
            try
            {
                var total = lines.Sum(l => l.LineTotal);
                var discount = lines.Sum(l =>
                {
                    var price = l.Product.Price;
                    var after = l.Product.PriceAfterDiscount ?? price;
                    return (price - after) * l.Quantity;
                });

                int orderId;
                using (var cmd = new MySqlCommand(
                    @"INSERT INTO orders(user_id, address, total_amount, discount_amount, created_at)
                      VALUES(@u,@a,@t,@d,NOW()); SELECT LAST_INSERT_ID();", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@u", userId);
                    cmd.Parameters.AddWithValue("@a", address ?? "");
                    cmd.Parameters.AddWithValue("@t", total);
                    cmd.Parameters.AddWithValue("@d", discount);
                    orderId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                foreach (var l in lines)
                {
                    var price = l.Product.Price;
                    var after = l.Product.PriceAfterDiscount ?? price;

                    using var it = new MySqlCommand(
                        @"INSERT INTO order_items(order_id, product_id, quantity, unit_price, discount_per_unit)
                          VALUES(@o,@p,@q,@up,@disc);", conn, tx);
                    it.Parameters.AddWithValue("@o", orderId);
                    it.Parameters.AddWithValue("@p", l.Product.Id);
                    it.Parameters.AddWithValue("@q", l.Quantity);
                    it.Parameters.AddWithValue("@up", price);
                    it.Parameters.AddWithValue("@disc", price - after);
                    it.ExecuteNonQuery();
                }

                // บันทึกตะกร้าปัจจุบันกลับไปด้วย (หรือจะเคลียร์ DB ก็ได้)
                using (var del = new MySqlCommand("DELETE FROM user_carts WHERE user_id=@u;", conn, tx))
                { del.Parameters.AddWithValue("@u", userId); del.ExecuteNonQuery(); }

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }
}
