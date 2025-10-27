using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiyunaKimono.Models
{
    public class TopPickItem : INotifyPropertyChanged
    {
        public int Id { get; set; }

        // --- เพิ่ม: เก็บ reference ไปยัง Product ต้นฉบับ ---
        public Product Source { get; private set; }
        private void Detach()
        {
            if (Source != null)
                Source.PropertyChanged -= Source_PropertyChanged;
            Source = null;
        }
        public void Attach(Product p)
        {
            Detach();
            Source = p;
            if (Source != null)
                Source.PropertyChanged += Source_PropertyChanged;

            // sync ครั้งแรก
            UpdateFrom(p);
        }
        private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // sync เฉพาะฟิลด์ที่สนใจ (ถ้าอยากครอบคลุมสุดก็เรียก UpdateFrom ทุกครั้ง)
            if (sender is Product p)
            {
                switch (e.PropertyName)
                {
                    case nameof(Product.Quantity):
                    case nameof(Product.Price):
                    case nameof(Product.Image1Path):
                    case nameof(Product.ProductName):
                    case nameof(Product.Category):
                    case nameof(Product.Discount):
                        UpdateFrom(p);
                        break;
                }
            }
        }
        // ------------------------------------------------------

        string _productName, _category, _image1Path, _offText;
        decimal _price;
        int _quantity;
        bool _isFavorite;

        public string ProductName { get => _productName; set => Set(ref _productName, value); }
        public string Category { get => _category; set => Set(ref _category, value); }
        public decimal Price { get => _price; set => Set(ref _price, value); }
        public int Quantity { get => _quantity; set => Set(ref _quantity, value); }
        public string Image1Path { get => _image1Path; set => Set(ref _image1Path, value); }
        public string OffText { get => _offText; set => Set(ref _offText, value); }
        public bool IsFavorite { get => _isFavorite; set => Set(ref _isFavorite, value); }

        public void UpdateFrom(Product p)
        {
            // sync ค่าจาก Product จริง
            ProductName = p.ProductName;
            Category = p.Category;
            Price = p.Price;
            Quantity = p.Quantity;
            Image1Path = p.Image1Path;

            if (p.Discount != 0m)
            {
                var discPercent = (int)System.Math.Round(p.Discount <= 1m ? p.Discount * 100m : p.Discount);
                OffText = discPercent > 0 ? $"{discPercent}% OFF" : null;
            }
            else
            {
                OffText = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        { if (Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
    }
}
