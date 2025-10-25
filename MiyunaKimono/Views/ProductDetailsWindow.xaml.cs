using MiyunaKimono.Models;
using MiyunaKimono.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace MiyunaKimono.Views
{
    public partial class ProductDetailsWindow : Window, INotifyPropertyChanged
    {
        public Product Product { get; }

        // Carousel
        public ObservableCollection<string> ImageList { get; } = new();
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(4) };

        private int _imageIndex;
        public int ImageIndex
        {
            get => _imageIndex;
            set { _imageIndex = value; OnPropertyChanged(); UpdateDots(); }
        }

        // Dot brushes (ง่าย ๆ)
        public Brush Dot1Brush { get; private set; } = Brushes.LightGray;
        public Brush Dot2Brush { get; private set; } = Brushes.LightGray;
        public Brush Dot3Brush { get; private set; } = Brushes.LightGray;

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                var v = Math.Max(1, Math.Min(Product.Quantity, value));
                _quantity = v; OnPropertyChanged();
            }
        }

        public string DiscountText
        {
            get
            {
                if (Product.Discount <= 0) return "0%";
                var pct = Product.Discount <= 1m ? Product.Discount * 100m : Product.Discount;
                return $"{(int)Math.Round(pct)}%";
            }
        }

        // ===== Heart toggle handlers (สำหรับการ์ดบนหน้า Home/List) =====
        private void Heart_Checked(object sender, RoutedEventArgs e)
        {
            var dc = (sender as FrameworkElement)?.DataContext;

            // การ์ดบนหน้า Home/List ใช้ TopPickItem ของ Models
            if (dc is MiyunaKimono.Models.TopPickItem t)
                FavoritesService.Instance.Set(t.Id, true);
            // เผื่อบางกรณีเป็น Product ตรง ๆ (เช่นหน้า Details)
            else if (dc is MiyunaKimono.Models.Product p)
                FavoritesService.Instance.Set(p.Id, true);
        }

        private void Heart_Unchecked(object sender, RoutedEventArgs e)
        {
            var dc = (sender as FrameworkElement)?.DataContext;

            if (dc is MiyunaKimono.Models.TopPickItem t)
                FavoritesService.Instance.Set(t.Id, false);
            else if (dc is MiyunaKimono.Models.Product p)
                FavoritesService.Instance.Set(p.Id, false);
        }

        public string PriceAfterText
        {
            get
            {
                var after = Product.PriceAfterDiscount ?? Product.Price;
                return $"{after:N0}  THB";
            }
        }

        public ProductDetailsWindow(Product product)
        {
            InitializeComponent();
            Product = product ?? throw new ArgumentNullException(nameof(product));
            DataContext = this;

            // รูป 3 รูป (วน)
            if (!string.IsNullOrWhiteSpace(Product.Image1Path)) ImageList.Add(Product.Image1Path);
            if (!string.IsNullOrWhiteSpace(Product.Image2Path)) ImageList.Add(Product.Image2Path);
            if (!string.IsNullOrWhiteSpace(Product.Image3Path)) ImageList.Add(Product.Image3Path);
            if (ImageList.Count == 0 && !string.IsNullOrWhiteSpace(Product.Image1Path))
                ImageList.Add(Product.Image1Path);

            ImageIndex = 0;
            _timer.Tick += (_, __) => { if (ImageList.Count > 0) ImageIndex = (ImageIndex + 1) % ImageList.Count; };
            _timer.Start();

            UpdateDots();
            QtyBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !char.IsDigit(e.Text, 0);
            };
        }

        private void UpdateDots()
        {
            Brush on = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            Brush off = new SolidColorBrush(Color.FromArgb(120, 200, 200, 200));
            Dot1Brush = ImageIndex % 3 == 0 ? on : off;
            Dot2Brush = ImageIndex % 3 == 1 ? on : off;
            Dot3Brush = ImageIndex % 3 == 2 ? on : off;
            OnPropertyChanged(nameof(Dot1Brush));
            OnPropertyChanged(nameof(Dot2Brush));
            OnPropertyChanged(nameof(Dot3Brush));
        }

        private void Back_Click(object sender, RoutedEventArgs e) => Close();

        private void Decrement_Click(object sender, RoutedEventArgs e) => Quantity--;
        private void Increment_Click(object sender, RoutedEventArgs e) => Quantity++;

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (Quantity > Product.Quantity)
            {
                MessageBox.Show("Not enough stock.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // เรียก Add ตามปกติ (คาดว่าเป็น void)
            CartService.Instance.Add(Product, Quantity);

            // แจ้งผลแบบง่าย ๆ
            MessageBox.Show("Added to cart", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // ปิดหน้ารายละเอียด (ไม่ปิดแอป)
            this.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}
