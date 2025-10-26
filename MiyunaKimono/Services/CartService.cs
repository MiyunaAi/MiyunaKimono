// Services/CartService.cs (เฉพาะส่วนสำคัญ)
using MiyunaKimono.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace MiyunaKimono.Services
{
    public class CartService : INotifyPropertyChanged
    {
        public static CartService Instance { get; } = new();

        public ObservableCollection<CartLine> Lines { get; } = new();

        public bool HasItems => Lines.Count > 0;
        public int TotalQuantity => Lines.Sum(l => l.Quantity);

        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public (bool ok, string msg) Add(Product product, int qty)
        {
            if (product == null) return (false, "ไม่พบสินค้า");
            if (qty < 1) qty = 1;

            var line = Lines.FirstOrDefault(l => l.Product.Id == product.Id);
            if (line == null)
            {
                line = new CartLine(product, qty);
                // ฟังการเปลี่ยนของแต่ละบรรทัด เพื่ออัปเดต badge/ยอดรวม
                line.PropertyChanged += (_, __) =>
                {
                    Raise(nameof(TotalQuantity));
                    Raise(nameof(HasItems));
                };
                Lines.Add(line);
            }
            else
            {
                line.Quantity += qty; // เปลี่ยนค่าที่ property -> UI จะเด้งเอง
            }

            Raise(nameof(TotalQuantity));
            Raise(nameof(HasItems));
            return (true, "เพิ่มสินค้าลงตะกร้าแล้ว");
        }

        public void SetQuantity(Product product, int qty)
        {
            var line = Lines.FirstOrDefault(l => l.Product.Id == product.Id);
            if (line == null) return;
            line.Quantity = qty; // สำคัญ: แก้ผ่าน property เดิม
            Raise(nameof(TotalQuantity));
        }

        public void Clear()
        {
            Lines.Clear();
            Raise(nameof(TotalQuantity));
            Raise(nameof(HasItems));
        }
    }
}
