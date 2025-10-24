// Services/CartService.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace MiyunaKimono.Services
{
    public class CartLine
    {
        public Models.Product Product { get; set; }
        public int Quantity { get; set; }

        // ใช้ราคาหลังหักส่วนลด (ถ้ามี) * จำนวน
        public decimal LineTotal => (Product.PriceAfterDiscount ?? Product.Price) * Quantity;
    }

    /// <summary>บริการตะกร้าแบบ Singleton ใช้ร่วมทั้งแอป</summary>
    public sealed class CartService : INotifyPropertyChanged
    {
        public static CartService Instance { get; } = new CartService();
        private CartService() { }

        public ObservableCollection<CartLine> Lines { get; } = new();

        private int _totalQuantity;
        public int TotalQuantity
        {
            get => _totalQuantity;
            private set
            {
                if (_totalQuantity == value) return;
                _totalQuantity = value;
                OnPropertyChanged(nameof(TotalQuantity));
                OnPropertyChanged(nameof(HasItems)); // ให้ XAML รีเฟรช Visibility ของ Badge
            }
        }

        public bool HasItems => TotalQuantity > 0;

        public (bool ok, string message) Add(Models.Product product, int qty)
        {
            if (qty <= 0) return (false, "จำนวนต้องมากกว่าศูนย์");
            if (qty > product.Quantity) return (false, "จำนวนมากเกินไป โปรดตรวจสอบจำนวนอีกครั้ง");

            var line = Lines.FirstOrDefault(l => l.Product.Id == product.Id);
            if (line == null)
            {
                Lines.Add(new CartLine { Product = product, Quantity = qty });
            }
            else
            {
                if (line.Quantity + qty > product.Quantity)
                    return (false, "จำนวนรวมเกินสต็อก โปรดลดจำนวน");
                line.Quantity += qty;
            }

            RecalcTotals();
            return (true, "เพิ่มลงตะกร้าสำเร็จ");
        }

        public void SetQuantity(Models.Product product, int qty)
        {
            var line = Lines.FirstOrDefault(l => l.Product.Id == product.Id);
            if (line == null) return;

            if (qty <= 0) { Lines.Remove(line); }
            else { line.Quantity = Math.Min(qty, product.Quantity); }

            RecalcTotals();
        }

        public void Remove(Models.Product product)
        {
            var line = Lines.FirstOrDefault(l => l.Product.Id == product.Id);
            if (line != null)
            {
                Lines.Remove(line);
                RecalcTotals();
            }
        }

        public void Clear()
        {
            Lines.Clear();
            RecalcTotals();
        }

        private void RecalcTotals()
        {
            TotalQuantity = Lines.Sum(l => l.Quantity);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
