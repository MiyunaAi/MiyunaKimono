// Models/TopPickItem.cs
using System.ComponentModel;

namespace MiyunaKimono.Models
{
    public class TopPickItem : INotifyPropertyChanged
    {
        public int Id { get; set; }                 // <— map มาจาก Product.Id

        public string ProductName { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image1Path { get; set; }
        public string OffText { get; set; }

        private bool _isFavorite;
        public bool IsFavorite                      // <— ใช้ผูกกับ ToggleButton หัวใจบนการ์ด
        {
            get => _isFavorite;
            set { if (_isFavorite == value) return; _isFavorite = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorite))); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
