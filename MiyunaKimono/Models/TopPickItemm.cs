using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/TopPickItem.cs
namespace MiyunaKimono.Models
{
    public class TopPickItem
    {
        public int Id { get; set; }

        public string ProductName { get; set; }
        public string Category { get; set; }

        public string Image1Path { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }
        public int Discount { get; set; } // เป็น % เช่น 10

        public bool IsFavorite { get; set; }

        // ข้อความประกอบ UI
        public string PriceText => $"{Price:N0} THB";
        //public string OffText => Discount > 0 ? $"{Discount} % off" : "";
        public string OffText { get; set; }
        public string HashCategory => string.IsNullOrWhiteSpace(Category) ? "—" : $"#{Category}";
    }
}

