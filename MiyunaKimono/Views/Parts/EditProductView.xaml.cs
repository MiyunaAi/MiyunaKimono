using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MiyunaKimono.Services;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using MiyunaKimono.Models;
using System.Collections.Generic;   // <-- เพิ่มอันนี้

namespace MiyunaKimono.Views.Parts
{
    public partial class EditProductView : UserControl, INotifyPropertyChanged
    {
        private readonly int _id;
        private readonly ProductService _service = new ProductService();

        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public event RoutedEventHandler RequestBack;
        public event RoutedEventHandler Saved;
        public event RoutedEventHandler Deleted;

        public ObservableCollection<ImageItem> Images { get; } = new();
        public ObservableCollection<string> StatusList { get; } = new();
        public ObservableCollection<string> BrandList { get; } = new();
        public ObservableCollection<string> CategoryList { get; } = new();

        // fields
        public string ProductName { get => _name; set { _name = value; Raise(); } }
        private string _name;
        public string ProductCode { get => _code; set { _code = value; Raise(); } }
        private string _code;
        public string Status { get => _status; set { _status = value; Raise(); } }
        private string _status;
        public string Brand { get => _brand; set { _brand = value; Raise(); } }
        private string _brand;
        public string Category { get => _cate; set { _cate = value; Raise(); } }
        private string _cate;
        public string Description { get => _desc; set { _desc = value; Raise(); } }
        private string _desc;
        public bool Visible { get => _visible; set { _visible = value; Raise(); } }
        private bool _visible = true;

        public string QuantityText { get => _qtyText; set { _qtyText = value; Raise(); } }
        private string _qtyText = "";
        public string PriceText { get => _priceText; set { _priceText = value; Raise(); } }
        private string _priceText = "";
        public string DiscountText { get => _discText; set { _discText = value; Raise(); } }
        private string _discText = "";

        public string LastUpdatedText { get => _last; set { _last = value; Raise(); } }
        private string _last;

        public ICommand RemoveImageCommand { get; }

        public EditProductView(int productId)
        {
            InitializeComponent();
            DataContext = this;
            _id = productId;
            RemoveImageCommand = new RelayCommand<ImageItem>(img => { if (img != null) Images.Remove(img); });
        }

        public async Task LoadAsync()
        {
            // lookups
            StatusList.Clear(); foreach (var s in await _service.GetStatusesAsync()) StatusList.Add(s);
            BrandList.Clear(); foreach (var s in await _service.GetBrandsAsync()) BrandList.Add(s);
            CategoryList.Clear(); foreach (var s in await _service.GetCategoriesAsync()) CategoryList.Add(s);

            // product
            var p = await _service.GetByIdAsync(_id);
            if (p == null) { MessageBox.Show("Product not found"); return; }

            ProductName = p.ProductName;
            ProductCode = p.ProductCode;
            Status = p.Status;
            Brand = p.Brand;
            Category = p.Category;
            Description = p.Description;
            Visible = p.Visible;

            QuantityText = p.Quantity.ToString(CultureInfo.InvariantCulture);
            PriceText = p.Price.ToString(CultureInfo.InvariantCulture);
            DiscountText = ((int)p.Discount).ToString(CultureInfo.InvariantCulture);

            Images.Clear();
            if (!string.IsNullOrWhiteSpace(p.Image1Path)) Images.Add(new ImageItem { Path = p.Image1Path });
            if (!string.IsNullOrWhiteSpace(p.Image2Path)) Images.Add(new ImageItem { Path = p.Image2Path });
            if (!string.IsNullOrWhiteSpace(p.Image3Path)) Images.Add(new ImageItem { Path = p.Image3Path });

            LastUpdatedText = p.UpdatedAt.HasValue
                ? $"Last time update: {p.UpdatedAt.Value:MMM dd, yyyy HH:mm}"
                : "Last time update: –";
        }

        private void UploadImage_Click(object s, RoutedEventArgs e)
        {
            if (Images.Count >= 3) { MessageBox.Show("รองรับได้สูงสุด 3 รูป"); return; }

            var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp", Multiselect = true };
            if (dlg.ShowDialog() == true)
            {
                foreach (var f in dlg.FileNames)
                {
                    if (Images.Count >= 3) break;
                    var uploads = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads");
                    Directory.CreateDirectory(uploads);
                    var name = Path.GetFileNameWithoutExtension(f);
                    var ext = Path.GetExtension(f);
                    var target = Path.Combine(uploads, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss_fff}{ext}");
                    File.Copy(f, target, overwrite: false);
                    Images.Add(new ImageItem { Path = target });
                }
            }
        }

        public async Task<bool> SaveAsync()
        {
            // parse numbers
            int.TryParse((QuantityText ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var qty);
            decimal.TryParse((PriceText ?? "").Replace(",", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var price);
            decimal.TryParse((DiscountText ?? "").Replace("%", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var discDec);
            var disc = (int)Math.Round(discDec, MidpointRounding.AwayFromZero);

            var dto = new MiyunaKimono.Models.ProductUpdateDto
            {
                Id = _id,
                ProductName = ProductName?.Trim(),
                ProductCode = ProductCode?.Trim(),
                Status = Status?.Trim(),
                Brand = Brand?.Trim(),
                Category = Category?.Trim(),
                Quantity = qty,
                Price = price,
                Discount = disc,
                Description = Description?.Trim(),
                Visible = Visible,
                ImagePaths = new List<string>()   // <-- ใช้แบบลิสต์
            };

            // เติมรูปลงลิสต์ (รองรับ 0–3 รูป)
            foreach (var img in Images)
            {
                if (!string.IsNullOrWhiteSpace(img?.Path))
                    dto.ImagePaths.Add(img.Path);
            }

            var ok = await _service.UpdateProductAsync(dto);
            if (ok)
            {
                MessageBox.Show("Update product success.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Saved?.Invoke(this, new RoutedEventArgs());
                return true;
            }
            MessageBox.Show("Update failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        public async Task<bool> DeleteAsync()
        {
            var ok = await _service.DeleteProductAsync(_id);
            if (ok)
            {
                MessageBox.Show("Delete product success.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Deleted?.Invoke(this, new RoutedEventArgs());
                return true;
            }
            MessageBox.Show("Delete failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}
