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
            ShowProduct(); // เริ่มที่ All Product
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

        // ===== Top bar handlers =====
        private void Back_Click(object sender, RoutedEventArgs e) => ShowProduct();

        private void AddProduct_Click(object s, RoutedEventArgs e) => ShowAddProduct();

        private async void Publish_Click(object s, RoutedEventArgs e)
        {
            if (_addView == null) return;
            var ok = await _addView.PublishAsync();
            if (ok) ShowProduct();
        }

        private async void Save_Click(object s, RoutedEventArgs e)
        {
            if (_editView == null) return;
            var ok = await _editView.SaveAsync();
            if (ok) ShowProduct();
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
            CurrentHeader = "Dashboard";
            ShowBackBtn = ShowAddBtn = ShowPublishBtn = ShowSaveBtn = ShowDeleteBtn = false;
            ContentHost.Content = new PlaceholderView { Title = "Dashboard" };
        }

        private void Orders_Click(object s, RoutedEventArgs e)
        {
            CurrentHeader = "Orders";
            ShowBackBtn = ShowAddBtn = ShowPublishBtn = ShowSaveBtn = ShowDeleteBtn = false;
            ContentHost.Content = new PlaceholderView { Title = "Orders" };
        }

        private void Report_Click(object s, RoutedEventArgs e)
        {
            CurrentHeader = "Report";
            ShowBackBtn = ShowAddBtn = ShowPublishBtn = ShowSaveBtn = ShowDeleteBtn = false;
            ContentHost.Content = new PlaceholderView { Title = "Report" };
        }

        private void Logout_Click(object s, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }
    }
}
