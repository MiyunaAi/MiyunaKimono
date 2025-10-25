using MiyunaKimono.Services;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiyunaKimono.Views
{
    public partial class CartView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public System.Collections.ObjectModel.ObservableCollection<CartLine> Lines
            => CartService.Instance.Lines;

        public int ItemsCount => Lines.Sum(l => l.Quantity);
        public string ItemsCountText => $"{ItemsCount} Item";

        public decimal DiscountTotal => Lines.Sum(l =>
        {
            var price = l.Product.Price;
            var after = l.Product.PriceAfterDiscount ?? price;
            return (price - after) * l.Quantity;
        });
        public string DiscountTotalText => $"{DiscountTotal:N0}";

        public decimal GrandTotal => Lines.Sum(l => l.LineTotal);
        public string GrandTotalText => $"{GrandTotal:N0}";

        public CartView()
        {
            InitializeComponent();
            DataContext = this;
            Lines.CollectionChanged += Lines_CollectionChanged;
            CartService.Instance.PropertyChanged += Instance_PropertyChanged;
        }

        private void Lines_CollectionChanged(object s, NotifyCollectionChangedEventArgs e) => RaiseTotals();
        private void Instance_PropertyChanged(object s, PropertyChangedEventArgs e)
        { if (e.PropertyName == nameof(CartService.TotalQuantity)) RaiseTotals(); }

        private void RaiseTotals()
        {
            PropertyChanged?.Invoke(this, new(nameof(ItemsCount)));
            PropertyChanged?.Invoke(this, new(nameof(ItemsCountText)));
            PropertyChanged?.Invoke(this, new(nameof(DiscountTotal)));
            PropertyChanged?.Invoke(this, new(nameof(DiscountTotalText)));
            PropertyChanged?.Invoke(this, new(nameof(GrandTotal)));
            PropertyChanged?.Invoke(this, new(nameof(GrandTotalText)));
        }

        // Stepper handlers
        private void DecrementLine_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is CartLine line)
                CartService.Instance.SetQuantity(line.Product, line.Quantity - 1);
        }

        private void IncrementLine_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is CartLine line)
                CartService.Instance.SetQuantity(line.Product, line.Quantity + 1);
        }

        private void Qty_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !char.IsDigit(e.Text, 0);

        private void Qty_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is CartLine line &&
                sender is TextBox tb &&
                int.TryParse(tb.Text, out var qty))
                CartService.Instance.SetQuantity(line.Product, qty);
        }

        // ให้ parent (UserMainWindow) เป็นคนสลับ section
        public event Action BackRequested;

        private void Back_Click(object sender, RoutedEventArgs e)
            => BackRequested?.Invoke();

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            var addr = AddressBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(addr))
            {
                MessageBox.Show("กรุณากรอกที่อยู่จัดส่ง", "Address required",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                int userId = AuthService.CurrentUserIdSafe();
                OrderService.Instance.CreateOrder(userId, addr, Lines.ToList());
                CartPersistenceService.Instance.Save(userId, Lines.ToList());

                MessageBox.Show("ทำรายการสั่งซื้อเรียบร้อย", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                CartService.Instance.Clear();
                BackRequested?.Invoke(); // กลับ Home
            }
            catch (Exception ex)
            {
                MessageBox.Show("บันทึกคำสั่งซื้อไม่สำเร็จ: " + ex.Message);
            }
        }
    }
}
