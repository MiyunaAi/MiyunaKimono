// Views/UserMainWindow.xaml.cs
using MiyunaKimono.Services;
using MiyunaKimono.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
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
