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

namespace MiyunaKimono.Views
{
    public partial class UserInfoView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // ====== โปรไฟล์ผู้ใช้ (ดึงจาก Session ที่คุณใช้อยู่แล้ว) ======
        public string FullName { get; private set; }
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string AvatarPath { get; private set; }  // ใส่ path รูปถ้ามี

        // ====== ออเดอร์ (ทั้งหมด + ที่กรองแล้ว) ======
        public ObservableCollection<OrderRow> AllOrders { get; } = new();
        public ObservableCollection<OrderRow> FilteredOrders { get; } = new();

        // ====== ตัวเลือกตัวกรอง เดือน/ปี ======
        public List<string> MonthOptions { get; } =
                new List<string> { "All" }
                .Concat(Enumerable.Range(1, 12)
                    .Select(i => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)))
                .ToList();

        public List<string> YearOptions { get; } =
            new List<string> { "All" }
            .Concat(Enumerable.Range(DateTime.Now.Year - 6, 9)
                .Select(y => y.ToString()))
            .ToList();


        private string _selectedMonth = "All";
        public string SelectedMonth
        {
            get => _selectedMonth;
            set { _selectedMonth = value; Raise(); ApplyFilter(); }
        }

        private string _selectedYear = "All";
        public string SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; Raise(); ApplyFilter(); }
        }

        // ====== อีเวนต์ส่งกลับไปหน้าหลัก ======
        public event Action BackRequested;

        public UserInfoView()
        {
            InitializeComponent();
            DataContext = this;
            LoadProfileFromSession();
        }

        private void LoadProfileFromSession()
        {
            // ดึงจาก Session (คุณมีอยู่แล้วในโปรเจกต์)
            var u = Session.CurrentUser;
            FullName = $"{u?.FirstName} {u?.LastName}".Trim();
            Username = u?.Username ?? "—";
            Email = u?.Email ?? "—";
            // AvatarPath: ถ้าคุณมี path ก็เซ็ตได้ (ค่าเริ่มต้นใช้ Assets/ic_user.png แล้ว)
            Raise(nameof(FullName)); Raise(nameof(Username)); Raise(nameof(Email)); Raise(nameof(AvatarPath));
        }

        // เรียกตอนเปิดหน้า/รีเฟรชหลังจากสั่งซื้อสำเร็จ
        public async Task ReloadAsync()
        {
            await LoadOrdersAsync();
            ApplyFilter();
        }

        private async Task LoadOrdersAsync()
        {
            AllOrders.Clear();

            var userId = AuthService.CurrentUserIdSafe();
            var orders = await OrderService.Instance.GetOrdersByUserAsync(userId);

            foreach (var o in orders.OrderByDescending(x => x.CreatedAt))
                AllOrders.Add(new OrderRow(o));
        }


        private void ApplyFilter()
        {
            FilteredOrders.Clear();
            IEnumerable<OrderRow> q = AllOrders;

            // ปี
            if (SelectedYear != "All" && int.TryParse(SelectedYear, out var y))
                q = q.Where(r => r.CreatedAt.Year == y);

            // เดือน
            if (SelectedMonth != "All")
            {
                int m = MonthOptions.FindIndex(s => s == SelectedMonth); // 1..12
                if (m >= 1) q = q.Where(r => r.CreatedAt.Month == m);
            }

            foreach (var r in q) FilteredOrders.Add(r);
        }

        private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();

        private void OpenDetails_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is OrderRow row)
            {
                // ตรงนี้คุณจะเปิดหน้ารายละเอียดออเดอร์จริงก็ได้
                MessageBox.Show($"Open order #{row.OrderId}", "Order", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    // ====== Row ที่ใช้โชว์บน UI ======
    public class OrderRow
    {
        // ในสคีมาใหม่ order_id เป็น string
        public string OrderId { get; }
        public string DisplayId => $"#{OrderId}";
        public string AmountText { get; }
        public string Status { get; }
        public DateTime CreatedAt { get; }
        public string DateText => CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        public OrderRow(OrderService.OrderInfo o)
        {
            OrderId = o.Id;
            AmountText = $"{o.Amount:N0} THB";
            Status = string.IsNullOrWhiteSpace(o.Status) ? "—" : o.Status;
            CreatedAt = o.CreatedAt;
        }
    }

}
