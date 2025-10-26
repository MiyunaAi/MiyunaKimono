// Models/CartLine.cs
using MiyunaKimono.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiyunaKimono.Services
{
    public class CartLine : INotifyPropertyChanged
    {
        public Product Product { get; }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                var v = value < 1 ? 1 : value;
                if (_quantity == v) return;
                _quantity = v;
                OnPropertyChanged();                 // Quantity
                OnPropertyChanged(nameof(LinePrice));
                OnPropertyChanged(nameof(LineTotal)); // ให้ UI refresh ราคา/ยอดรวม
            }
        }

        // ราคาต่อชิ้นหลังลด (ถ้าไม่ลดใช้ราคาปกติ)
        public decimal UnitPrice => Product.PriceAfterDiscount ?? Product.Price;

        // ชื่อที่ XAML ใช้อยู่
        public decimal LinePrice => UnitPrice;              // ราคา/ชิ้น
        public decimal LineTotal => UnitPrice * Quantity;   // ราคารวม

        public CartLine(Product product, int quantity = 1)
        {
            Product = product ?? throw new ArgumentNullException(nameof(product));
            Quantity = quantity < 1 ? 1 : quantity;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
