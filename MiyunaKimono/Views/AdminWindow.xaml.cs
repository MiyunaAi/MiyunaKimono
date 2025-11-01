using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using MiyunaKimono.Views.Parts;

namespace MiyunaKimono.Views
{
    public partial class AdminWindow : Window, INotifyPropertyChanged
    {
        public AdminWindow()
        {
            InitializeComponent();
            DataContext = this;
            // ✅ เปลี่ยนจาก ShowProduct() เป็น ShowDashboard()
            ShowDashboard(); // เริ่มที่ Dashboard
        }



        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // ===== Header =====
        private string _currentHeader = "All Product";
        public string CurrentHeader { get => _currentHeader; set { _currentHeader = value; OnPropertyChanged(); } }

        // ===== ปุ่มใน Top Bar =====
        private bool _showBackBtn;
        public bool ShowBackBtn { get => _showBackBtn; set { _showBackBtn = value; OnPropertyChanged(); } }

        private bool _showAddBtn;
        public bool ShowAddBtn { get => _showAddBtn; set { _showAddBtn = value; OnPropertyChanged(); } }

        private bool _showPublishBtn;
        public bool ShowPublishBtn { get => _showPublishBtn; set { _showPublishBtn = value; OnPropertyChanged(); } }

        private bool _showSaveBtn;
        public bool ShowSaveBtn { get => _showSaveBtn; set { _showSaveBtn = value; OnPropertyChanged(); } }

        private bool _showDeleteBtn;
        public bool ShowDeleteBtn { get => _showDeleteBtn; set { _showDeleteBtn = value; OnPropertyChanged(); } }

        // views
        private AddProductView _addView;
        private EditProductView _editView;
        private AllOrdersView _allOrdersView;
        private AdminOrderDetailsView _adminOrderDetailsView;
        private ReportView _reportView;
        private DashboardView _dashboardView;
        // ===== Navigation =====
        public void ShowProduct()
        {
            CurrentHeader = "All Product";
            ShowBackBtn = false;
            ShowAddBtn = true;
            ShowPublishBtn = false;
            ShowSaveBtn = false;
            ShowDeleteBtn = false;

            ContentHost.Content = new ProductView();
            _addView = null; _editView = null;
        }

        public void ShowAddProduct()
        {
            _addView = new AddProductView();
            _addView.Published += (_, __) => ShowProduct();
            _addView.RequestBack += (_, __) => ShowProduct();

            CurrentHeader = "Add New Product";
            ShowBackBtn = true;   // แสดง back
            ShowAddBtn = false;
            ShowPublishBtn = true;
            ShowSaveBtn = false;
            ShowDeleteBtn = false;

            ContentHost.Content = _addView;
            _editView = null;
        }

        public async Task ShowEditProductAsync(int productId)
        {
            _editView = new EditProductView(productId);
            _editView.Saved += (_, __) => ShowProduct();
            _editView.Deleted += (_, __) => ShowProduct();
            _editView.RequestBack += (_, __) => ShowProduct();

            CurrentHeader = "Edit Product";
            ShowBackBtn = true;   // แสดง back
            ShowAddBtn = false;
            ShowPublishBtn = false;
            ShowSaveBtn = true;
            ShowDeleteBtn = true;

            await _editView.LoadAsync();
            ContentHost.Content = _editView;
            _addView = null;
        }

        public void ShowOrders()
        {
            CurrentHeader = "All Orders";
            ShowBackBtn = false;
            ShowAddBtn = false;
            ShowPublishBtn = false;
            ShowSaveBtn = false;
            ShowDeleteBtn = false;

            if (_allOrdersView == null)
            {
                _allOrdersView = new AllOrdersView();
                _allOrdersView.ViewDetailsRequested += async (orderId) => await ShowAdminOrderDetailsAsync(orderId);
            }

            ContentHost.Content = _allOrdersView;
            _addView = null; _editView = null; _adminOrderDetailsView = null;
        }

        // --- 🔽 3. เพิ่มเมธอด ShowAdminOrderDetailsAsync 🔽 ---
        public async Task ShowAdminOrderDetailsAsync(string orderId)
        {
            _adminOrderDetailsView = new AdminOrderDetailsView(orderId);
            _adminOrderDetailsView.RequestBack += () => ShowOrders();
            _adminOrderDetailsView.Saved += () => ShowOrders();

            CurrentHeader = $"Order #{orderId}";
            ShowBackBtn = false; // (ปุ่ม Back อยู่ใน UserControl เอง)
            ShowAddBtn = false;
            ShowPublishBtn = false;
            ShowSaveBtn = false; // (ปุ่ม Save อยู่ใน UserControl เอง)
            ShowDeleteBtn = false;

            await _adminOrderDetailsView.LoadAsync();
            ContentHost.Content = _adminOrderDetailsView;
            _addView = null; _editView = null; _allOrdersView = null;
        }

        public void ShowReport()
        {
            CurrentHeader = "Reports Product";
            ShowBackBtn = false;
            ShowAddBtn = false;
            ShowPublishBtn = false;
            ShowSaveBtn = false;
            ShowDeleteBtn = false;

            if (_reportView == null)
            {
                _reportView = new ReportView();
            }

            ContentHost.Content = _reportView;
            _addView = null; _editView = null; _allOrdersView = null; _adminOrderDetailsView = null;
        }

        public void ShowDashboard()
        {
            CurrentHeader = "Dashboard";
            ShowBackBtn = false;
            ShowAddBtn = false;
            ShowPublishBtn = false;
            ShowSaveBtn = false;
            ShowDeleteBtn = false;

            if (_dashboardView == null)
            {
                _dashboardView = new DashboardView();
                // เชื่อมปุ่ม Details จากตาราง "Recent Transactions"
                _dashboardView.ViewDetailsRequested += async (orderId) => await ShowAdminOrderDetailsAsync(orderId);
            }

            ContentHost.Content = _dashboardView;
            _addView = null; _editView = null; _allOrdersView = null; _adminOrderDetailsView = null; _reportView = null;
        }

        // ===== Top bar handlers =====
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // (เช็คว่าควรกลับไปหน้าไหน)
            if (_editView != null || _addView != null)
            {
                ShowProduct();
            }
            else
            {
                ShowProduct(); // ค่าเริ่มต้น
            }
        }

        private void AddProduct_Click(object s, RoutedEventArgs e) => ShowAddProduct();

        private async void Publish_Click(object s, RoutedEventArgs e)
        {
            if (_addView == null) return;
            var ok = await _addView.PublishAsync();
            if (ok) ShowProduct();
        }

        private async void Save_Click(object s, RoutedEventArgs e)
        {
            if (_editView != null)
            {
                var ok = await _editView.SaveAsync();
                if (ok) ShowProduct();
            }
            else if (_adminOrderDetailsView != null) // ⬅️ เพิ่มเช็คนี้
            {
                var ok = await _adminOrderDetailsView.SaveAsync();
                if (ok) ShowOrders(); // ⬅️ กลับไปหน้า All Orders
            }
        }

        private async void Delete_Click(object s, RoutedEventArgs e)
        {
            if (_editView == null) return;

            if (MessageBox.Show("Delete this product?", "Confirm",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                var ok = await _editView.DeleteAsync();
                if (ok) ShowProduct();
            }
        }

        // ===== Left menu =====
        private void Product_Click(object s, RoutedEventArgs e) => ShowProduct();

        private void Dashboard_Click(object s, RoutedEventArgs e)
        {
            ShowDashboard(); // ⬅️ เรียกเมธอดใหม่
        }

        private void Orders_Click(object s, RoutedEventArgs e)
        {
            ShowOrders(); // ⬅️ เรียกเมธอดใหม่
        }

        private void Report_Click(object s, RoutedEventArgs e)
        {
            ShowReport(); // ⬅️ เรียกเมธอดใหม่
        }

        private void Logout_Click(object s, RoutedEventArgs e)
        {
            // ล้าง session ตามต้องการ
            MiyunaKimono.Services.Session.CurrentUser = null;
            MiyunaKimono.Services.AuthService.SetCurrentUserId(0);
            // (ถ้าต้องการ) ล้าง cart/favorite ของ user ปัจจุบัน
            // CartPersistenceService.Instance.Clear();

            var login = new LoginWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // ชี้ MainWindow ไปที่หน้าล็อกอินก่อน
            Application.Current.MainWindow = login;

            login.Show();
            this.Close();   // ตอนนี้ปิด AdminWindow ได้โดยไม่ปิดทั้งแอป
        }

    }
}