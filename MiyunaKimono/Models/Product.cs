using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiyunaKimono.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Status { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string Description { get; set; }

        // รูป
        public string Image1Path { get; set; }
        public string Image2Path { get; set; }
        public string Image3Path { get; set; }

        // >>> เพิ่มสองตัวนี้ <<<
        public bool Visible { get; set; } = true;     // แสดง/ซ่อนสินค้า
        public DateTime? UpdatedAt { get; set; }      // เวลาอัปเดตล่าสุด (null ได้)

        // เผื่อโชว์ thumbnail ใน Grid (ใช้รูปแรก)
        public string ThumbPath => string.IsNullOrWhiteSpace(Image1Path) ? null : Image1Path;
    }
}