// Services/CartLine.cs
using MiyunaKimono.Models;

namespace MiyunaKimono.Services
{
    public class CartLine
    {
        public Product Product { get; set; }   // ต้องมี Models.Product อยู่แล้ว
        public int Quantity { get; set; }
        public decimal LineTotal
        {
            get
            {
                if (Product == null) return 0m;
                var price = Product.Price;
                var after = Product.PriceAfterDiscount ?? price;
                return after * Quantity;
            }
        }
    }
}
