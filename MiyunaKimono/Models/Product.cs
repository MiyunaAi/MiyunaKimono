using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiyunaKimono.Models
{
    public class Product : INotifyPropertyChanged
    {
        // --- คีย์/ไม่ค่อยเปลี่ยน จะเป็น auto-property ก็ได้ ---
        public int Id { get; set; }
        public string ProductCode { get; set; }

        // --- พร็อพที่มีผลต่อ UI: ใช้ pattern Set<T> ---
        string _productName, _status, _brand, _category;
        int _quantity;
        decimal _price, _discount;
        string _description;
        string _image1Path, _image2Path, _image3Path;
        bool _visible = true;
        DateTime? _updatedAt;
        bool _isFavorite;

        public string ProductName { get => _productName; set => Set(ref _productName, value); }
        public string Status { get => _status; set => Set(ref _status, value); }
        public string Brand { get => _brand; set => Set(ref _brand, value); }
        public string Category { get => _category; set => Set(ref _category, value); }

        public int Quantity { get => _quantity; set => Set(ref _quantity, value); }
        public decimal Price { get => _price; set => Set(ref _price, value); }
        public decimal Discount { get => _discount; set => Set(ref _discount, value); }
        public string Description { get => _description; set => Set(ref _description, value); }

        public string Image1Path { get => _image1Path; set => Set(ref _image1Path, value); }
        public string Image2Path { get => _image2Path; set => Set(ref _image2Path, value); }
        public string Image3Path { get => _image3Path; set => Set(ref _image3Path, value); }

        public bool Visible { get => _visible; set => Set(ref _visible, value); }
        public DateTime? UpdatedAt { get => _updatedAt; set => Set(ref _updatedAt, value); }

        public bool IsFavorite { get => _isFavorite; set => Set(ref _isFavorite, value); }

        // Thumbnail ช่วยอ่านค่าจาก Image1Path
        public string ThumbPath => string.IsNullOrWhiteSpace(Image1Path) ? null : Image1Path;

        public decimal? PriceAfterDiscount
        {
            get
            {
                if (Discount <= 0) return null;
                var pct = Discount <= 1m ? Discount : Discount / 100m;
                return Math.Round(Price * (1 - pct), 2, MidpointRounding.AwayFromZero);
            }
        }

        // === ใช้โดย ProductService.Attach(...) หรือเวลาต้องอัปเดตออบเจ็กต์เดิม ===
        public void ApplyFrom(Product s)
        {
            // เรียงจากสิ่งที่ UI ใช้บ่อย
            Quantity = s.Quantity;
            Price = s.Price;
            Discount = s.Discount;

            ProductName = s.ProductName;
            Category = s.Category;
            Status = s.Status;
            Brand = s.Brand;

            Description = s.Description;

            Image1Path = s.Image1Path;
            Image2Path = s.Image2Path;
            Image3Path = s.Image3Path;

            Visible = s.Visible;
            UpdatedAt = s.UpdatedAt;

            // ตัวเลือก: sync favorite ด้วย ถ้าต้องการ
            IsFavorite = s.IsFavorite;

            // ถ้าคุณมี binding กับ PriceAfterDiscount/ThumbPath ใน XAML
            OnPropertyChanged(nameof(PriceAfterDiscount));
            OnPropertyChanged(nameof(ThumbPath));
        }

        // === INotifyPropertyChanged boilerplate ===
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}