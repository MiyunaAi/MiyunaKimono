using MiyunaKimono.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System; // 
using System.Collections.Generic; //
using System.Linq; //

namespace MiyunaKimono.Views.Parts
{
    public partial class TopSellingView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // Event สำหรับแจ้ง AdminWindow ให้ย้อนกลับ
        public event Action RequestBack;

        // ViewModel ที่คัดลอกมาจาก DashboardView.xaml.cs
        public ObservableCollection<TopSellingProductViewModel> TopSellingItems { get; } = new ObservableCollection<TopSellingProductViewModel>();

        public TopSellingView()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                TopSellingItems.Clear();

                // เรียก Service โดยระบุ Limit จำนวนมากๆ เพื่อให้ได้ "All"
                var topProducts = await OrderService.Instance.GetTopSellingProductsAsync(1000);

                int topIndex = 1;
                foreach (var p in topProducts)
                {
                    TopSellingItems.Add(new TopSellingProductViewModel(p, topIndex++));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load top selling products: " + ex.Message, "Error");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            RequestBack?.Invoke(); // ยิง Event กลับไปหา AdminWindow
        }
    }
}