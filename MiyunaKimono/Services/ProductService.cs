// Services/ProductService.cs
using MiyunaKimono.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MiyunaKimono.Services
{
    // ✅ เหลือเฉพาะ DTO สร้างสินค้าไว้ที่ Services ก็ได้
    // (อัปเดตสินค้าให้ไปใช้ Models.ProductUpdateDto เท่านั้น)
    public class ProductCreateDto
    {
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string Status { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int Discount { get; set; } // เก็บเป็นเปอร์เซ็นต์หรือจำนวนบาทตามตารางจริง
        public string Description { get; set; }
        public bool Visible { get; set; }
        public List<string> ImagePaths { get; } = new();
    }

    public class ProductService
    {
        // ===== อัปเดตสินค้า (รับ DTO จาก Models เท่านั้น) =====
        public async Task<bool> UpdateProductAsync(ProductUpdateDto dto)
        {
            using var conn = Db.GetConn();

            string img1 = dto.ImagePaths.Count > 0 ? dto.ImagePaths[0] : null;
            string img2 = dto.ImagePaths.Count > 1 ? dto.ImagePaths[1] : null;
            string img3 = dto.ImagePaths.Count > 2 ? dto.ImagePaths[2] : null;

            const string sql = @"
                UPDATE products SET
                    product_code = @code,
                    product_name = @name,
                    status       = @status,
                    brand        = @brand,
                    category     = @category,
                    quantity     = @qty,
                    price        = @price,
                    discount     = @disc,
                    description  = @desc,
                    visible      = @visible,
                    image1_path  = @img1,
                    image2_path  = @img2,
                    image3_path  = @img3,
                    updated_at   = NOW()
                WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", dto.Id);
            cmd.Parameters.AddWithValue("@code", (object?)dto.ProductCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@name", (object?)dto.ProductName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", (object?)dto.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@brand", (object?)dto.Brand ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@category", (object?)dto.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@qty", dto.Quantity);
            cmd.Parameters.AddWithValue("@price", dto.Price);
            cmd.Parameters.AddWithValue("@disc", dto.Discount);
            cmd.Parameters.AddWithValue("@desc", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@visible", dto.Visible);
            cmd.Parameters.AddWithValue("@img1", (object?)img1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@img2", (object?)img2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@img3", (object?)img3 ?? DBNull.Value);

            var n = await cmd.ExecuteNonQueryAsync();
            return n > 0;
        }

        // ===== ลบสินค้า =====
        public async Task<bool> DeleteProductAsync(int id)
        {
            using var conn = Db.GetConn();
            const string sql = "DELETE FROM products WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            var n = await cmd.ExecuteNonQueryAsync();
            return n > 0;
        }

        // ===== ใช้กับ ProductView และหน้า User =====
        public List<Product> GetAll()
        {
            var list = new List<Product>();

            using (var conn = Db.GetConn())
            using (var cmd = new MySqlCommand(@"
                SELECT 
                    id,
                    product_code,
                    product_name,
                    category,
                    price,
                    quantity,
                    status,
                    brand,
                    description,
                    visible,
                    image1_path,
                    image2_path,
                    image3_path,
                    discount,
                    updated_at
                FROM products
                WHERE (deleted_at IS NULL OR deleted_at = 0)
                ORDER BY id DESC;", conn))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    var p = new Product
                    {
                        Id = rd.GetInt32("id"),
                        ProductCode = rd["product_code"]?.ToString(),
                        ProductName = rd["product_name"]?.ToString(),
                        Category = rd["category"]?.ToString(),
                        Price = rd["price"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["price"]),
                        Quantity = rd["quantity"] == DBNull.Value ? 0 : Convert.ToInt32(rd["quantity"]),
                        Status = rd["status"]?.ToString(),
                        Brand = rd["brand"]?.ToString(),
                        Description = rd["description"]?.ToString(),
                        Visible = rd["visible"] != DBNull.Value && Convert.ToBoolean(rd["visible"]),
                        Image1Path = rd["image1_path"]?.ToString(),
                        Image2Path = rd["image2_path"]?.ToString(),
                        Image3Path = rd["image3_path"]?.ToString(),
                        Discount = rd["discount"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["discount"]),
                        UpdatedAt = rd["updated_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["updated_at"])
                    };
                    list.Add(p);
                }
            }

            return list;
        }

        // ===== ดึง Top Picks (map -> Models.TopPickItem) =====
        // หมายเหตุ: ไม่อ้างพร็อพที่อาจไม่มีในโมเดล เช่น DiscountPercent เพื่อเลี่ยง error
        public async Task<List<TopPickItem>> GetTopPicksAsync(int count = 8)
        {
            var products = await GetRandomAsync(count);
            var list = new List<TopPickItem>();

            foreach (var p in products)
            {
                // คิด % ส่วนลดจากคอลัมน์ discount (decimal) -> int
                var discPercent = (int)Math.Round(p.Discount, MidpointRounding.AwayFromZero);

                list.Add(new TopPickItem
                {
                    ProductName = p.ProductName,
                    Category = p.Category,
                    Price = (decimal)p.Price,    // ถ้า TopPickItem.Price เป็น double
                    Quantity = p.Quantity,
                    Image1Path = p.Image1Path,       // <--- ใส่คอมมาให้ครบ
                    OffText = discPercent > 0 ? $"{discPercent}% OFF" : null
                });
            }

            return list;
        }

        // ===== Lookup dropdown =====
        public Task<List<string>> GetStatusesAsync() => Task.Run(() => DistinctString("status"));
        public Task<List<string>> GetBrandsAsync() => Task.Run(() => DistinctString("brand"));
        public Task<List<string>> GetCategoriesAsync() => Task.Run(() => DistinctString("category"));

        private List<string> DistinctString(string column)
        {
            var res = new List<string>();
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand($@"
                SELECT DISTINCT {column}
                FROM products
                WHERE {column} IS NOT NULL AND {column} <> ''
                ORDER BY {column};", conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var s = rd[0] as string;
                if (!string.IsNullOrWhiteSpace(s)) res.Add(s);
            }
            return res;
        }

        // ===== Create =====
        public Task<bool> CreateProductAsync(ProductCreateDto dto) => Task.Run(() =>
        {
            using var conn = Db.GetConn();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new MySqlCommand(@"
                    INSERT INTO products
                      (product_code, product_name, status, brand, category,
                       quantity, price, discount, description,
                       image1_path, image2_path, image3_path, visible, updated_at)
                    VALUES
                      (@code, @name, @status, @brand, @category,
                       @qty, @price, @discount, @desc,
                       @img1, @img2, @img3, @visible, NOW());
                    SELECT LAST_INSERT_ID();", conn, tx);

                cmd.Parameters.AddWithValue("@code", dto.ProductCode ?? "");
                cmd.Parameters.AddWithValue("@name", dto.ProductName ?? "");
                cmd.Parameters.AddWithValue("@status", dto.Status ?? "");
                cmd.Parameters.AddWithValue("@brand", dto.Brand ?? "");
                cmd.Parameters.AddWithValue("@category", dto.Category ?? "");
                cmd.Parameters.AddWithValue("@qty", dto.Quantity);
                cmd.Parameters.AddWithValue("@price", dto.Price);
                cmd.Parameters.AddWithValue("@discount", dto.Discount);
                cmd.Parameters.AddWithValue("@desc", dto.Description ?? "");
                cmd.Parameters.AddWithValue("@img1", dto.ImagePaths.Count > 0 ? dto.ImagePaths[0] : null);
                cmd.Parameters.AddWithValue("@img2", dto.ImagePaths.Count > 1 ? dto.ImagePaths[1] : null);
                cmd.Parameters.AddWithValue("@img3", dto.ImagePaths.Count > 2 ? dto.ImagePaths[2] : null);
                cmd.Parameters.AddWithValue("@visible", dto.Visible);

                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                tx.Commit();
                return newId > 0;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });

        // ===== Read by Id =====
        public async Task<Product> GetByIdAsync(int id)
        {
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand(@"
                SELECT id, product_code, product_name, status, brand, category,
                       quantity, price, discount, description,
                       image1_path, image2_path, image3_path, visible, updated_at
                FROM products
                WHERE id=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync()) return null;

            return new Product
            {
                Id = rd.GetInt32("id"),
                ProductCode = rd["product_code"]?.ToString(),
                ProductName = rd["product_name"]?.ToString(),
                Status = rd["status"] as string,
                Brand = rd["brand"] as string,
                Category = rd["category"] as string,
                Quantity = rd["quantity"] == DBNull.Value ? 0 : Convert.ToInt32(rd["quantity"]),
                Price = rd["price"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["price"]),
                Discount = rd["discount"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["discount"]),
                Description = rd["description"] as string,
                Image1Path = rd["image1_path"] as string,
                Image2Path = rd["image2_path"] as string,
                Image3Path = rd["image3_path"] as string,
                Visible = rd["visible"] != DBNull.Value && Convert.ToBoolean(rd["visible"]),
                UpdatedAt = rd["updated_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["updated_at"])
            };
        }

        // ===== Update (แบบใช้ ProductCreateDto เดิม) / Delete =====
        public Task<bool> UpdateAsync(int id, ProductCreateDto dto) => Task.Run(() =>
        {
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand(@"
                UPDATE products SET
                  product_code=@code, product_name=@name, status=@status, brand=@brand, category=@category,
                  quantity=@qty, price=@price, discount=@discount, description=@desc,
                  image1_path=@img1, image2_path=@img2, image3_path=@img3, visible=@visible,
                  updated_at=NOW()
                WHERE id=@id;", conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@code", dto.ProductCode ?? "");
            cmd.Parameters.AddWithValue("@name", dto.ProductName ?? "");
            cmd.Parameters.AddWithValue("@status", dto.Status ?? "");
            cmd.Parameters.AddWithValue("@brand", dto.Brand ?? "");
            cmd.Parameters.AddWithValue("@category", dto.Category ?? "");
            cmd.Parameters.AddWithValue("@qty", dto.Quantity);
            cmd.Parameters.AddWithValue("@price", dto.Price);
            cmd.Parameters.AddWithValue("@discount", dto.Discount);
            cmd.Parameters.AddWithValue("@desc", dto.Description ?? "");
            cmd.Parameters.AddWithValue("@img1", dto.ImagePaths.Count > 0 ? dto.ImagePaths[0] : null);
            cmd.Parameters.AddWithValue("@img2", dto.ImagePaths.Count > 1 ? dto.ImagePaths[1] : null);
            cmd.Parameters.AddWithValue("@img3", dto.ImagePaths.Count > 2 ? dto.ImagePaths[2] : null);
            cmd.Parameters.AddWithValue("@visible", dto.Visible);

            return cmd.ExecuteNonQuery() > 0;
        });

        public Task<bool> DeleteAsync(int id) => Task.Run(() =>
        {
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand("DELETE FROM products WHERE id=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        });

        // ===== ใช้กับหน้า User: สุ่ม N รายการ =====
        public async Task<List<Product>> GetRandomAsync(int count = 4)
        {
            var list = new List<Product>();

            using (var conn = Db.GetConn())
            using (var cmd = new MySqlCommand(@"
                SELECT id, product_code, product_name, category,
                       price, discount, quantity, image1_path
                FROM products
                WHERE (deleted_at IS NULL OR deleted_at = 0)
                ORDER BY RAND()
                LIMIT @n;", conn))
            {
                cmd.Parameters.AddWithValue("@n", count);

                using (var rd = await cmd.ExecuteReaderAsync())
                {
                    while (await rd.ReadAsync())
                    {
                        list.Add(new Product
                        {
                            Id = rd.GetInt32("id"),
                            ProductCode = rd["product_code"]?.ToString(),
                            ProductName = rd["product_name"]?.ToString(),
                            Category = rd["category"]?.ToString(),
                            Price = rd["price"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["price"]),
                            Discount = rd["discount"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["discount"]),
                            Quantity = rd["quantity"] == DBNull.Value ? 0 : Convert.ToInt32(rd["quantity"]),
                            Image1Path = rd["image1_path"]?.ToString()
                        });
                    }
                }
            }

            return list;
        }
    }
}
