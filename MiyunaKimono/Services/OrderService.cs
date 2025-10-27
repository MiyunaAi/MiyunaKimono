using MiyunaKimono.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiyunaKimono.Services
{
    public class OrderService
    {
        public static OrderService Instance { get; } = new();

        // ====== เวอร์ชันเดิม (ยังใช้ได้) ======
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

                int orderAutoId;
                using (var cmd = new MySqlCommand(
                    @"INSERT INTO orders(user_id, address, total_amount, discount_amount, created_at)
                      VALUES(@u,@a,@t,@d,NOW()); SELECT LAST_INSERT_ID();", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@u", userId);
                    cmd.Parameters.AddWithValue("@a", address ?? "");
                    cmd.Parameters.AddWithValue("@t", total);
                    cmd.Parameters.AddWithValue("@d", discount);
                    orderAutoId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                foreach (var l in lines)
                {
                    var price = l.Product.Price;
                    var after = l.Product.PriceAfterDiscount ?? price;

                    using var it = new MySqlCommand(
                        @"INSERT INTO order_items(order_id, product_id, quantity, unit_price, discount_per_unit)
                          VALUES(@o,@p,@q,@up,@disc);", conn, tx);
                    it.Parameters.AddWithValue("@o", orderAutoId);
                    it.Parameters.AddWithValue("@p", l.Product.Id);
                    it.Parameters.AddWithValue("@q", l.Quantity);
                    it.Parameters.AddWithValue("@up", price);
                    it.Parameters.AddWithValue("@disc", price - after);
                    it.ExecuteNonQuery();
                }

                using (var del = new MySqlCommand("DELETE FROM user_carts WHERE user_id=@u;", conn, tx))
                { del.Parameters.AddWithValue("@u", userId); del.ExecuteNonQuery(); }

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ====== เวอร์ชันเต็มที่คุณต้องการ (เก็บข้อมูลลูกค้า/สถานะ/สลิป/ไฟล์ ฯลฯ) ======
        public class NewOrderLine
        {
            public string ProductName { get; set; }
            public int Qty { get; set; }
            public decimal Price { get; set; }   // ราคาต่อชิ้น (ราคาเต็ม)
            public decimal Total { get; set; }   // QTY * (ราคาหลังลดของคุณ ถ้าต้องการ)
        }

        /// <summary>
        /// สร้างออเดอร์แบบครบถ้วน: gen order_id (text), เก็บข้อมูลลูกค้า/สลิป, สร้าง order_items
        /// คืนค่า order_id (เช่น "ORD20251027152301")
        /// </summary>
        public async Task<string> CreateOrderFullAsync(
            int userId,
            string customerFullName,
            string username,
            string address,
            string tel,
            List<CartLine> lines,
            decimal total,
            decimal discount,
            byte[] receiptBytes,
            string receiptFileName)
        {
            // order_id แบบอ่านง่าย (text key) ตามที่คุณระบุ
            var id = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss");

            using var conn = Db.GetConn(); // สมมุติเปิดแล้ว
            using var tx = await conn.BeginTransactionAsync();

            // บันทึกหัวออเดอร์
            var cmd = new MySqlCommand(@"
INSERT INTO orders
(order_id, user_id, customer_name, username, address, tel,
 amount, discount, status, created_at, receipt_file_name, receipt_content)
VALUES(@id,@uid,@cname,@uname,@addr,@tel,@amt,@disc,'Ordering',NOW(),@fn,@file);
", conn, (MySqlTransaction)tx);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@cname", customerFullName ?? "");
            cmd.Parameters.AddWithValue("@uname", username ?? "");
            cmd.Parameters.AddWithValue("@addr", address ?? "");
            cmd.Parameters.AddWithValue("@tel", tel ?? "");
            cmd.Parameters.AddWithValue("@amt", total);
            cmd.Parameters.AddWithValue("@disc", discount);
            cmd.Parameters.AddWithValue("@fn", string.IsNullOrWhiteSpace(receiptFileName) ? "receipt" : receiptFileName);
            cmd.Parameters.AddWithValue("@file", receiptBytes ?? Array.Empty<byte>());
            await cmd.ExecuteNonQueryAsync();

            // บันทึกรายการสินค้า
            foreach (var l in lines)
            {
                // 1) บันทึก order_items (ของเดิม)
                var cmd2 = new MySqlCommand(@"
        INSERT INTO order_items(order_id, product_name, qty, price, total)
        VALUES(@id,@name,@q,@p,@t)", conn, (MySqlTransaction)tx);
                cmd2.Parameters.AddWithValue("@id", id);
                cmd2.Parameters.AddWithValue("@name", l.Product.ProductName);
                cmd2.Parameters.AddWithValue("@q", l.Quantity);
                cmd2.Parameters.AddWithValue("@p", l.Product.Price);
                cmd2.Parameters.AddWithValue("@t", l.LineTotal);
                await cmd2.ExecuteNonQueryAsync();

                // 2) ล็อกแถวสินค้า แล้วเช็คสต็อก
                var lockCmd = new MySqlCommand(
                    "SELECT quantity FROM products WHERE id=@pid FOR UPDATE",
                    conn, (MySqlTransaction)tx);
                lockCmd.Parameters.AddWithValue("@pid", l.Product.Id);
                var currentQtyObj = await lockCmd.ExecuteScalarAsync();
                var currentQty = currentQtyObj == null ? 0 : Convert.ToInt32(currentQtyObj);

                if (currentQty < l.Quantity)
                    throw new Exception($"สินค้า '{l.Product.ProductName}' คงเหลือไม่พอ (เหลือ {currentQty}, ต้องการ {l.Quantity})");

                // 3) ตัดสต็อก
                var upd = new MySqlCommand(
                    "UPDATE products SET quantity = quantity - @q WHERE id=@pid",
                    conn, (MySqlTransaction)tx);
                upd.Parameters.AddWithValue("@q", l.Quantity);
                upd.Parameters.AddWithValue("@pid", l.Product.Id);
                await upd.ExecuteNonQueryAsync();
            }


            await tx.CommitAsync();
            return id;
        }
    }
}
