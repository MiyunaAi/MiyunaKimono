using MiyunaKimono.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiyunaKimono.Services
{
    public class ProductService
    {
        public List<Product> GetAll()
        {
            var list = new List<Product>();
            using var conn = Db.GetConn();
            using var cmd = new MySqlCommand(@"
                SELECT id, product_code, product_name, status, brand, category,
                       quantity, price, discount, description,
                       image1_path, image2_path, image3_path
                FROM products
                ORDER BY id ASC", conn);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new Product
                {
                    Id = rd.GetInt32("id"),
                    ProductCode = rd.GetString("product_code"),
                    ProductName = rd.GetString("product_name"),
                    Status = rd["status"] as string,
                    Brand = rd["brand"] as string,
                    Category = rd["category"] as string,
                    Quantity = rd.GetInt32("quantity"),
                    Price = rd.GetDecimal("price"),
                    Discount = rd.GetDecimal("discount"),
                    Description = rd["description"] as string,
                    Image1Path = rd["image1_path"] as string,
                    Image2Path = rd["image2_path"] as string,
                    Image3Path = rd["image3_path"] as string
                });
            }
            return list;
        }
    }
}