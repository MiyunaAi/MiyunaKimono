// Views/UserMainWindow.xaml.cs
using MiyunaKimono.Models;
using MiyunaKimono.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
// ป้องกันชื่อซ้อนกับคลาสอื่น
using TopPickItemModel = MiyunaKimono.Models.TopPickItem;

namespace MiyunaKimono.Views
{
    public partial class UserMainWindow : Window, INotifyPropertyChanged
    {
        // วางในคลาส UserMainWindow (อยู่นอกเมธอดอื่น ๆ)
        private void TopPicksHost_Loaded(object sender, RoutedEventArgs e)
        {
            // เรียกคำนวณจำนวนการ์ดที่พอดี 1 แถว
            ReflowTopPicks();
        }

        private void TopPicksHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // เรียกคำนวณใหม่เมื่อขนาดเปลี่ยน
            ReflowTopPicks();
        }


        // ====== Data for UI ======
        public ObservableCollection<TopPickItemModel> TopPicks { get; } = new();
        public ObservableCollection<string> HeroImages { get; } = new();

        private int _heroIndex;
        public int HeroIndex
        {
            get => _heroIndex;
            set { _heroIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentHeroImage)); }
        }

        public string CurrentHeroImage => HeroImages.Count == 0 ? null : HeroImages[HeroIndex];

        private readonly DispatcherTimer _timer;

        // Services
        private readonly ProductService _productSvc = new();

        // Commands (ใช้กับปุ่มลูกศรใน HeroCarousel)
        public ICommand PrevHeroCommand { get; }
        public ICommand NextHeroCommand { get; }

        public UserMainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // ----- Commands -----
            PrevHeroCommand = new DelegateCommand(_ => PrevHero());
            NextHeroCommand = new DelegateCommand(_ => NextHero());

            // ----- Hero images -----
            HeroImages.Add("pack://application:,,,/Assets/hero1.jpg");
            HeroImages.Add("pack://application:,,,/Assets/hero2.jpg");
            HeroImages.Add("pack://application:,,,/Assets/hero3.jpg");
            HeroImages.Add("pack://application:,,,/Assets/hero4.jpg");
            HeroIndex = 0;

            // Auto-rotate hero
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _timer.Tick += (_, __) => NextHero();
            _timer.Start();

            // โหลด Top Picks ตอน Loaded (กัน UI ค้าง)
            Loaded += async (_, __) => await SafeLoadTopPicksAsync();
            Unloaded += (_, __) => _timer.Stop();

            // ----- Hero #2 images (อีกชุดรูป) -----
            HeroImages2.Add("pack://application:,,,/Assets/hero5.jpg");
            HeroImages2.Add("pack://application:,,,/Assets/hero6.jpg");
            HeroImages2.Add("pack://application:,,,/Assets/hero7.jpg");
            HeroImages2.Add("pack://application:,,,/Assets/hero8.jpg");
            HeroIndex2 = 0;

            // คำสั่ง Prev/Next ของ Hero #2
            PrevHero2Command = new DelegateCommand(_ => PrevHero2());
            NextHero2Command = new DelegateCommand(_ => NextHero2());

            // Auto-rotate hero #2
            _timer2 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _timer2.Tick += (_, __) => NextHero2();
            _timer2.Start();

            // เมื่อหน้า Loaded ให้เริ่ม/หยุด timer ให้ครบทั้ง 2 ตัว (มีของเดิมอยู่แล้วสำหรับ hero #1)
            Unloaded += (_, __) => _timer2.Stop();



        }

        // ====== สำหรับหน้า List ======
        public ObservableCollection<TopPickItem> FilteredProducts { get; } = new();

        private string _listTitle;
        public string ListTitle
        {
            get => _listTitle;
            set { _listTitle = value; OnPropertyChanged(); }
        }

        private int _listCount;
        public int ListCount
        {
            get => _listCount;
            set { _listCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ListCountText)); }
        }

        public string ListCountText => $"({ListCount} Products Available)";

        // cache ข้อมูลทั้งหมดไว้ (ดึงทีเดียวแล้ว reuse)
        private List<Product> _allDbProducts;
        private bool _loadedAll;

        private void ShowHome()
        {
            HomeSection.Visibility = Visibility.Visible;
            ListSection.Visibility = Visibility.Collapsed;
        }

        private void ShowList()
        {
            HomeSection.Visibility = Visibility.Collapsed;
            ListSection.Visibility = Visibility.Visible;
        }

        private async Task EnsureAllProductsAsync()
        {
            if (_loadedAll) return;
            // ดึงจาก service (ของคุณมี GetAll())
            _allDbProducts = _productSvc.GetAll();   // synchronous ในโค้ดคุณ
            _loadedAll = true;
        }

        private static TopPickItem MapToTopPick(Product p)
        {
            // ถ้า TopPickItem.Price เป็น double ให้ cast ให้ตรง
            // สมมุติ TopPickItem.Price เป็น decimal ตามที่เราใช้ในการ์ด
            var price = p.Price; // decimal
                                 // คิด % off ถ้าคุณเก็บ discount เป็นจำนวนเงิน/เปอร์เซ็นต์ ปรับตามจริง
                                 // ตัวอย่างนี้ถือว่า discount เป็นเปอร์เซ็นต์ 0..100
            string offText = null;
            if (p.Discount != 0m)
            {
                // ถ้า p.Discount เก็บเป็นเปอร์เซ็นต์ (decimal)
                var discPercent = (int)Math.Round(p.Discount);
                offText = discPercent > 0 ? $"{discPercent}% OFF" : null;
            }

            return new TopPickItem
            {
                ProductName = p.ProductName,
                Category = p.Category,
                Price = price,           // <= ให้ชนิดตรงกับพร็อพใน TopPickItem ของคุณ
                Quantity = p.Quantity,
                Image1Path = p.Image1Path,
                OffText = offText
            };
        }

        private async Task ShowAllProductsAsync()
        {
            await EnsureAllProductsAsync();

            FilteredProducts.Clear();
            foreach (var p in _allDbProducts)
                FilteredProducts.Add(MapToTopPick(p));

            ListTitle = "All Product";
            ListCount = FilteredProducts.Count;
            ShowList();
        }

        private async Task ShowCategoryAsync(string category)
        {
            await EnsureAllProductsAsync();

            FilteredProducts.Clear();
            foreach (var p in _allDbProducts)
            {
                if (string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase))
                    FilteredProducts.Add(MapToTopPick(p));
            }

            // ---- ตั้งชื่อหัวข้อ: บางหมวดไม่เติมคำว่า "Kimono"
            var noSuffix = category.Equals("Yukata", StringComparison.OrdinalIgnoreCase)
                        || category.Equals("Accessories", StringComparison.OrdinalIgnoreCase);

            ListTitle = noSuffix ? category : $"{category} Kimono";
            // -----------------------------------------------

            ListCount = FilteredProducts.Count;
            ShowList();
        }


        private void Nav_Home_Click(object sender, RoutedEventArgs e)
        {
            ShowHome();
        }

        private async void Nav_All_Click(object sender, RoutedEventArgs e)
        {
            await ShowAllProductsAsync();
        }

        private async void Nav_Furisode_Click(object sender, RoutedEventArgs e)
        {
            await ShowCategoryAsync("Furisode");
        }

        // (ถ้ามีปุ่มหมวดอื่น ก็เพิ่ม method คู่กัน เช่น Nav_Homongi_Click => ShowCategoryAsync("Homongi"))
        private async void Nav_Homongi_Click(object sender, RoutedEventArgs e)
            => await ShowCategoryAsync("Homongi");

        private async void Nav_Hakama_Click(object sender, RoutedEventArgs e)
            => await ShowCategoryAsync("Hakama");

        private async void Nav_Kurotomesode_Click(object sender, RoutedEventArgs e)
            => await ShowCategoryAsync("Kurotomesode");

        private async void Nav_Shiromuku_Click(object sender, RoutedEventArgs e)
            => await ShowCategoryAsync("Shiromuku");

        private async void Nav_Yukata_Click(object sender, RoutedEventArgs e)
            => await ShowCategoryAsync("Yukata");

        private async void Nav_Accessories_Click(object sender, RoutedEventArgs e)
            => await ShowCategoryAsync("Accessories");


        private async void CategoryIcon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string category && !string.IsNullOrWhiteSpace(category))
            {
                await ShowCategoryAsync(category);
            }
        }

        // ข้อมูลสำหรับหน้า All Products
        public ObservableCollection<TopPickItemModel> AllProducts { get; } = new();

        // โหลดทั้งหมดจากฐานข้อมูล แล้ว map เป็นการ์ดแบบเดียวกับ Top Picks
        private async Task LoadAllProductsAsync()
        {
            AllProducts.Clear();

            var products = await Task.Run(() => _productSvc.GetAll());

            foreach (var p in products)
            {
                // สร้าง OffText จาก Discount ถ้ามี (สมมติในตารางเก็บ %)
                string off = null;
                if (p.Discount is decimal d && d > 0)
                {
                    // ถ้าเก็บเป็น 0-1 ให้แปลงเป็น %
                    var discPercent = d <= 1m ? (int)Math.Round(d * 100m) : (int)Math.Round(d);
                    off = $"{discPercent}% OFF";
                }

                AllProducts.Add(new TopPickItemModel
                {
                    ProductName = p.ProductName,
                    Category = p.Category,
                    Price = (decimal)p.Price,
                    Quantity = p.Quantity,
                    Image1Path = p.Image1Path,
                    OffText = off
                });
            }
        }

        private async void AllKimono_Click(object sender, RoutedEventArgs e)
        {
            await ShowAllProductsAsync();   // ไปใช้ ListSection
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            ShowHome();                     // กลับมา HomeSection
        }

        private void Furisode_Click(object sender, RoutedEventArgs e)
        {
            new ProductListWindow("Furisode Kimono", "Furisode").Show();
            Close(); // ถ้าต้องการปิดหน้า Home เดิม
        }

        private void CF(object sender, RoutedEventArgs e)
        {
            new ProductListWindow("All Product", null).Show();
            Close();
        }


        // ===== Hero #2 =====
        public ObservableCollection<string> HeroImages2 { get; } = new();

                    private int _heroIndex2;
                    public int HeroIndex2
                    {
                        get => _heroIndex2;
                        set { _heroIndex2 = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentHeroImage2)); }
                    }
                    public string CurrentHeroImage2 => HeroImages2.Count == 0 ? null : HeroImages2[HeroIndex2];

                    private readonly DispatcherTimer _timer2;
                    public ICommand PrevHero2Command { get; }
                    public ICommand NextHero2Command { get; }

                    private void NextHero2()
                    {
                        if (HeroImages2.Count == 0) return;
                        HeroIndex2 = (HeroIndex2 + 1) % HeroImages2.Count;
                    }

                    private void PrevHero2()
                    {
                        if (HeroImages2.Count == 0) return;
                        HeroIndex2 = (HeroIndex2 - 1 + HeroImages2.Count) % HeroImages2.Count;
                    }


        

        // ====== Top Picks ======
        private async Task SafeLoadTopPicksAsync()
        {
            try { await LoadTopPicksAsync(); }
            catch (Exception ex)
            {
                MessageBox.Show("Load Top Picks failed: " + ex.Message);
            }
        }

        private async Task LoadTopPicksAsync()
        {
            TopPicks.Clear();

            // ใช้บริการของคุณ (ถ้ามี) ดึงรายการ Top Picks 8 ชิ้น
            // ถ้ายังไม่มีเมธอดนี้ ให้เปลี่ยนเป็น GetAll() แล้ว .Take(8) แทน
            var items = await _productSvc.GetTopPicksAsync(6);

            foreach (TopPickItemModel it in items)
                TopPicks.Add(it);
        }

        // ====== Hero control ======
        private void NextHero()
        {
            if (HeroImages.Count == 0) return;
            HeroIndex = (HeroIndex + 1) % HeroImages.Count;
        }
        // เพิ่มในคลาส UserMainWindow
        private void ReflowTopPicks()
        {
            // TODO: ถ้าจะคำนวณจำนวนการ์ดต่อแถวค่อยมาเติม logic ทีหลัง
        }

        private void PrevHero()
        {
            if (HeroImages.Count == 0) return;
            HeroIndex = (HeroIndex - 1 + HeroImages.Count) % HeroImages.Count;
        }

        // (เผื่อถ้าปุ่มใน XAML ยังใช้ Click อยู่)
        private void NextHero_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            NextHero();
            _timer.Start();
        }

        private void PrevHero_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            PrevHero();
            _timer.Start();
        }

        // ปุ่ม "More >" ในส่วน Top Picks
        private void MoreTop_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Coming soon.");
        }

        // ====== INotifyPropertyChanged ======
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ====== คำสั่งง่าย ๆ สำหรับผูกกับปุ่ม (DelegateCommand) ======
    internal class DelegateCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public DelegateCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        // ===== Hero #2 =====


        

    }
}
