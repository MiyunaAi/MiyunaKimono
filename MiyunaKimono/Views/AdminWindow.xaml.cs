using MiyunaKimono.Views.Parts;
using MiyunaKimono.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;

namespace MiyunaKimono.Views
{
    public partial class AdminWindow : Window, INotifyPropertyChanged
    {
        // ====== Properties ให้ TopBar ผูก ======
        private string _currentHeader;
        public string CurrentHeader
        {
            get => _currentHeader;
            set { _currentHeader = value; OnPropertyChanged(nameof(CurrentHeader)); }
        }

        private bool _isProductTab;
        public bool IsProductTab
        {
            get => _isProductTab;
            set { _isProductTab = value; OnPropertyChanged(nameof(IsProductTab)); }
        }
        // ======================================

        public AdminWindow()
        {
            InitializeComponent();

            // ให้ Top Bar (Text/Visibility) ผูกกับพร็อพฯ ในคลาสนี้
            DataContext = this;

            // หน้าเปิดเริ่มต้น (ถ้าอยากเริ่มที่ Dashboard ให้เปลี่ยนเป็น ShowDashboard())
            ShowProduct();
        }

        // ---------- เมนูซ้าย: เปลี่ยนหน้า + ตั้งค่าหัวข้อ/ปุ่ม ----------
        private void ShowDashboard()
        {
            CurrentHeader = "Dashboard";
            IsProductTab = false;                           // ซ่อนปุ่ม Add
            ContentHost.Content = new PlaceholderView();     // เนื้อหา placeholder
        }

        private void ShowOrders()
        {
            CurrentHeader = "Orders";
            IsProductTab = false;
            ContentHost.Content = new PlaceholderView();
        }

        private void ShowReport()
        {
            CurrentHeader = "Report";
            IsProductTab = false;
            ContentHost.Content = new PlaceholderView();
        }

        private void ShowProduct()
        {
            CurrentHeader = "All Product";
            IsProductTab = true;                            // โชว์ปุ่ม Add new Product
            ContentHost.Content = new ProductView();         // ตารางสินค้า
        }
        // ---------------------------------------------------------------

        // ปุ่มในเมนูซ้าย (ผูกจาก XAML)
        private void Dashboard_Click(object s, RoutedEventArgs e) => ShowDashboard();
        private void Product_Click(object s, RoutedEventArgs e) => ShowProduct();
        private void Orders_Click(object s, RoutedEventArgs e) => ShowOrders();
        private void Report_Click(object s, RoutedEventArgs e) => ShowReport();

        // ปุ่ม Add new Product บน Top Bar (ตอนนี้ยังไม่ทำอะไร ใส่ dialog/หน้าเพิ่มสินค้าภายหลังได้)
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            // TODO: เปิดหน้าสร้างสินค้าใหม่ หรือ dialog
            MessageBox.Show("Add new Product (coming soon)");
        }

        private void Logout_Click(object s, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
