using System;
using System.Collections.Generic;
using System.ComponentModel;
using MySql.Data.MySqlClient;

namespace MiyunaKimono.Services
{
    /// <summary>
    /// เก็บ/โหลดสินค้าที่ผู้ใช้กดหัวใจ (Favorites) จากฐานข้อมูล (MySQL) แบบ per-user
    /// ต้องเรียก InitForUser(userId) หลัง login เสมอ
    /// </summary>
    public sealed class FavoritesService : INotifyPropertyChanged
    {
        public static FavoritesService Instance { get; } = new FavoritesService();
        private FavoritesService() { }

        private readonly HashSet<int> _ids = new();
        private int? _userId;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public int? CurrentUserId => _userId;
        public int Count => _ids.Count;

        /// <summary>
        /// ต้องเรียกทุกครั้งหลัง login สำเร็จ (และตอนสลับ user)
        /// </summary>
        public void InitForUser(int userId)
        {
            _userId = userId;
            LoadFromDb();
            OnPropertyChanged(nameof(Count));
        }

        /// <summary>
        /// เคลียร์เมื่อ logout (optional)
        /// </summary>
        public void ClearUser()
        {
            _userId = null;
            _ids.Clear();
            OnPropertyChanged(nameof(Count));
        }

        public bool IsFavorite(int productId) => _ids.Contains(productId);

        /// <summary>
        /// ตั้งค่า Favorite และ sync ลง DB ทันที
        /// </summary>
        public void Set(int productId, bool isFav)
        {
            if (_userId == null) return; // ยังไม่ได้ login → ไม่บันทึก

            var changed = false;

            if (isFav)
            {
                if (_ids.Add(productId))
                {
                    InsertDb(productId);
                    changed = true;
                }
            }
            else
            {
                if (_ids.Remove(productId))
                {
                    DeleteDb(productId);
                    changed = true;
                }
            }

            if (changed) OnPropertyChanged(nameof(Count));
        }

        // ===== DB I/O =====

        private void LoadFromDb()
        {
            _ids.Clear();
            if (_userId == null) return;

            using var conn = Db.GetConn(); // ใช้คลาส Db ของโปรเจกต์คุณ
            using var cmd = new MySqlCommand(
                "SELECT product_id FROM user_favorites WHERE user_id=@uid;",
                conn);
            cmd.Parameters.AddWithValue("@uid", _userId.Value);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var pid = Convert.ToInt32(rd["product_id"]);
                _ids.Add(pid);
            }
        }

        private void InsertDb(int productId)
        {
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand(
                "INSERT IGNORE INTO user_favorites(user_id, product_id) VALUES(@uid, @pid);",
                conn);
            cmd.Parameters.AddWithValue("@uid", _userId.Value);
            cmd.Parameters.AddWithValue("@pid", productId);
            cmd.ExecuteNonQuery();
        }

        private void DeleteDb(int productId)
        {
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand(
                "DELETE FROM user_favorites WHERE user_id=@uid AND product_id=@pid;",
                conn);
            cmd.Parameters.AddWithValue("@uid", _userId.Value);
            cmd.Parameters.AddWithValue("@pid", productId);
            cmd.ExecuteNonQuery();
        }
    }
}
