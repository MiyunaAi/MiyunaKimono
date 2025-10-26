using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;

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
        private int _wiredUserId = 0;

        // เรียกหลัง login สำเร็จ
        public void WireUpAutosave(int userId)
        {
            _wiredUserId = userId;

            // ครั้งแรกแนบ handler ให้ทุกบรรทัด
            foreach (var l in CartService.Instance.Lines)
                l.PropertyChanged += Line_PropertyChanged;

            // เปลี่ยนแปลงจำนวนบรรทัด (เพิ่ม/ลบ)
            CartService.Instance.Lines.CollectionChanged += Lines_CollectionChanged;
        }

        // เรียกตอน logout (หรือเปลี่ยนผู้ใช้) เพื่อถอด handler
        public void UnwireAutosave()
        {
            CartService.Instance.Lines.CollectionChanged -= Lines_CollectionChanged;
            foreach (var l in CartService.Instance.Lines)
                l.PropertyChanged -= Line_PropertyChanged;

            _wiredUserId = 0;
        }

        // === Handlers ===
        private void Lines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (CartLine l in e.NewItems) l.PropertyChanged += Line_PropertyChanged;

            if (e.OldItems != null)
                foreach (CartLine l in e.OldItems) l.PropertyChanged -= Line_PropertyChanged;

            // บันทึกทุกครั้งที่แถวเปลี่ยน
            if (_wiredUserId > 0)
                Save(_wiredUserId, CartService.Instance.Lines.ToList());
        }

        private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartLine.Quantity))
            {
                if (_wiredUserId > 0)
                    Save(_wiredUserId, CartService.Instance.Lines.ToList());
            }
        }


    }
}
    