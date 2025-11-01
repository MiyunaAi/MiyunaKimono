using MiyunaKimono.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MiyunaKimono.Views.Parts
{
    public partial class DashboardView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // Event สำหรับปุ่ม Details
        public event Action<string> ViewDetailsRequested;

        // --- Properties สำหรับ 4 การ์ดบน ---
        private decimal _totalSales;
        public decimal TotalSales
        {
            get => _totalSales;
            set { _totalSales = value; Raise(); }
        }

        private int _totalOrders;
        public int TotalOrders
        {
            get => _totalOrders;
            set { _totalOrders = value; Raise(); }
        }

        private int _totalCustomers;
        public int TotalCustomers
        {
            get => _totalCustomers;
            set { _totalCustomers = value; Raise(); }
        }

        // --- Dropdowns ---
        public List<string> MonthOptions { get; }
        public List<string> YearOptions { get; }
        public string SelectedMonth { get; set; } = "All Month";
        public string SelectedYear { get; set; } = "All Year";

        // --- ตาราง ---
        public ObservableCollection<TopSellingProductViewModel> TopSellingItems { get; } = new ObservableCollection<TopSellingProductViewModel>();
        public ObservableCollection<RecentTransactionViewModel> RecentTransactions { get; } = new ObservableCollection<RecentTransactionViewModel>();

        public DashboardView()
        {
            InitializeComponent();
            DataContext = this;

            // (เหมือนหน้า Report)
            MonthOptions = new List<string> { "All Month" };
            MonthOptions.AddRange(Enumerable.Range(1, 12)
                .Select(i => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)));

            YearOptions = new List<string> { "All Year" };
            int currentBEYear = new ThaiBuddhistCalendar().GetYear(DateTime.Now);
            YearOptions.AddRange(Enumerable.Range(currentBEYear - 5, 6)
                .Select(y => y.ToString()).Reverse());

            SelectedMonth = MonthOptions.First();
            SelectedYear = YearOptions.First();

            Loaded += async (s, e) => await LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // 1. โหลด 3 การ์ดบน
                var stats = await OrderService.Instance.GetDashboardStatsAsync();
                TotalSales = stats.TotalSales;
                TotalOrders = stats.TotalOrders;
                TotalCustomers = await UserService.Instance.GetTotalCustomerCountAsync();

                // 2. โหลด Top Selling
                TopSellingItems.Clear();
                var topProducts = await OrderService.Instance.GetTopSellingProductsAsync(10);
                int topIndex = 1;
                foreach (var p in topProducts)
                {
                    TopSellingItems.Add(new TopSellingProductViewModel(p, topIndex++));
                }

                // 3. โหลด Recent/Expensive Transactions
                RecentTransactions.Clear();
                var transactions = await OrderService.Instance.GetMostExpensiveTransactionsAsync(10);
                foreach (var t in transactions)
                {
                    RecentTransactions.Add(new RecentTransactionViewModel(t));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load dashboard data: " + ex.Message, "Error");
            }
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string orderId)
            {
                ViewDetailsRequested?.Invoke(orderId); // ⬅️ ยิง Event
            }
        }
    }

    // --- ViewModel สำหรับตาราง Top Selling ---
    public class TopSellingProductViewModel
    {
        private readonly TopSellingProduct _source;
        public int Index { get; }
        public string ProductCode => $"#{_source.ProductCode}";
        public string ProductName => _source.ProductName;
        public string Category => _source.Category;
        public string PriceText => $"{_source.Price:N0} THB";
        public string TotalOrdersText => $"{_source.TotalOrders:N0}";
        public string TotalSaleText => $"{_source.TotalSale:N0} THB";

        public TopSellingProductViewModel(TopSellingProduct source, int index)
        {
            _source = source;
            Index = index;
        }
    }

    // --- ViewModel สำหรับตาราง Recent Transaction ---
    // (เราใช้ Model เดียวกับ AllOrdersView ได้เลย)
    public class RecentTransactionViewModel
    {
        private readonly OrderService.AdminOrderInfo _source;
        public string OrderId => _source.Id;
        public string DisplayId => $"#{_source.Id}";
        public string CustomerName => _source.CustomerName ?? "N/A";
        public string AmountText => $"{_source.Amount:N0} THB";
        public string Status => string.IsNullOrWhiteSpace(_source.Status) ? "Ordering" : _source.Status;
        public string DateText => _source.CreatedAt.ToString("dd MMM yyyy");

        public RecentTransactionViewModel(OrderService.AdminOrderInfo source)
        {
            _source = source;
        }
    }
}