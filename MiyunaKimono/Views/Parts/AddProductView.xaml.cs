using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MiyunaKimono.Services;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Globalization;

namespace MiyunaKimono.Views.Parts
{
    public partial class AddProductView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public event RoutedEventHandler RequestBack;
        public event RoutedEventHandler Published;

        public ObservableCollection<ImageItem> Images { get; } = new();
        public ObservableCollection<string> StatusList { get; } = new();
        public ObservableCollection<string> BrandList { get; } = new();
        public ObservableCollection<string> CategoryList { get; } = new();

        // ฟิลด์ข้อความทั่วไป
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

        // ช่องตัวเลข -> เก็บเป็น string แล้วค่อยแปลงตอน Publish
        public string QuantityText { get => _qtyText; set { _qtyText = value; Raise(); } }
        private string _qtyText = "";
        public string PriceText { get => _priceText; set { _priceText = value; Raise(); } }
        private string _priceText = "";
        public string DiscountText { get => _discText; set { _discText = value; Raise(); } }
        private string _discText = "";

        public ICommand RemoveImageCommand { get; }

        private readonly ProductService _service = new ProductService();

        public AddProductView()
        {
            InitializeComponent();
            DataContext = this;

            RemoveImageCommand = new RelayCommand<ImageItem>(img =>
            {
                if (img != null) Images.Remove(img);
            });

            _ = LoadLookupsAsync();
        }

        // ============== โหลด dropdown lookup ==============
        private async Task LoadLookupsAsync()
        {
            StatusList.Clear(); foreach (var s in await _service.GetStatusesAsync()) StatusList.Add(s);
            BrandList.Clear(); foreach (var s in await _service.GetBrandsAsync()) BrandList.Add(s);
            CategoryList.Clear(); foreach (var s in await _service.GetCategoriesAsync()) CategoryList.Add(s);
        }

        // ============== ปุ่ม Back บนหน้านี้ (ถ้ามี) ==============
        private void Back_Click(object s, RoutedEventArgs e) =>
            RequestBack?.Invoke(this, new RoutedEventArgs());

        // ============== อัพโหลดรูป (สูงสุด 3) ==============
        private void UploadImage_Click(object s, RoutedEventArgs e)
        {
            if (Images.Count >= 3) { MessageBox.Show("รองรับได้สูงสุด 3 รูป"); return; }

            var dlg = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true)
            {
                foreach (var f in dlg.FileNames)
                {
                    if (Images.Count >= 3) break;

                    var root = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "MiyunaKimono", "products");
                    Directory.CreateDirectory(root);
                    var uploads = root;

                    // กันชื่อซ้ำ -> เติม timestamp
                    var name = Path.GetFileNameWithoutExtension(f);
                    var ext = Path.GetExtension(f);
                    var target = Path.Combine(uploads, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss_fff}{ext}");

                    File.Copy(f, target, overwrite: false);
                    Images.Add(new ImageItem { Path = target });
                }
            }
        }

        // ============== ปุ่ม Publish (ถ้าเรียกจากปุ่มภายในหน้านี้เอง) ==============
        private async void Publish_Click(object s, RoutedEventArgs e)
        {
            var ok = await PublishAsync();
            if (ok) Published?.Invoke(this, new RoutedEventArgs()); // ให้ AdminWindow เด้งกลับ
        }

        // ============== เมธอดที่ AdminWindow เรียก: await _addView.PublishAsync() ==============
        public async Task<bool> PublishAsync()
        {
            try
            {
                // 1) validate ขั้นต่ำ
                if (string.IsNullOrWhiteSpace(ProductName))
                {
                    MessageBox.Show("กรุณากรอก Product Name", "Warning");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(ProductCode))
                {
                    MessageBox.Show("กรุณากรอก Product Code", "Warning");
                    return false;
                }

                // 2) แปลงค่าตัวเลขจาก Text (ยอมรับว่าง/คอมม่า/%)
                if (!int.TryParse((QuantityText ?? "").Trim(),
                                  NumberStyles.Integer,
                                  CultureInfo.InvariantCulture,
                                  out var qty)) qty = 0;

                var priceSan = (PriceText ?? "").Replace(",", "").Trim();
                if (!decimal.TryParse(priceSan, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    price = 0m;

                var discSan = (DiscountText ?? "").Replace("%", "").Trim();
                if (!decimal.TryParse(discSan, NumberStyles.Any, CultureInfo.InvariantCulture, out var discountDec))
                    discountDec = 0m;

                // discount เก็บเป็น int ฝั่ง DB? ถ้าใช่ปัดเป็น int
                var discountInt = (int)Math.Round(discountDec, MidpointRounding.AwayFromZero);

                // 3) ยัดข้อมูลลง DTO
                var dto = new ProductCreateDto
                {
                    ProductName = ProductName?.Trim(),
                    ProductCode = ProductCode?.Trim(),
                    Status = Status?.Trim(),
                    Brand = Brand?.Trim(),
                    Category = Category?.Trim(),
                    Quantity = qty,
                    Price = price,
                    Discount = discountInt,
                    Description = Description?.Trim(),
                    Visible = Visible
                };
                foreach (var img in Images) dto.ImagePaths.Add(img.Path);

                // 4) บันทึก
                await _service.CreateProductAsync(dto);   // แค่รอให้ทำงานเสร็จ (ถ้าพังจะ throw)


                MessageBox.Show("บันทึกสำเร็จ", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("บันทึกไม่สำเร็จ: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }

    public class ImageItem { public string Path { get; set; } }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _run;
        public RelayCommand(Action<T> run) => _run = run;
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _run((T)p);
        public event EventHandler CanExecuteChanged;
    }
}
