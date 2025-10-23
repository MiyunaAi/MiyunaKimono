// Models/ProductUpdateDto.cs
using System.Collections.Generic;

namespace MiyunaKimono.Models
{
    public class ProductUpdateDto
    {
        public int Id { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Status { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int Discount { get; set; } // เก็บเป็นเปอร์เซ็นต์
        public string Description { get; set; }
        public bool Visible { get; set; }
        public List<string> ImagePaths { get; set; } = new();
    }
}
