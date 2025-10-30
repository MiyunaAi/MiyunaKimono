﻿using MiyunaKimono.Models;
using MiyunaKimono.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MiyunaKimono.Views
{
    public partial class OrderDetailsView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public event Action BackRequested;

        private readonly string _orderId;
        private OrderDetailsModel _details;

        // --- Properties for Binding ---
        public string OrderId => _details?.OrderId ?? "Loading...";
        public string DisplayId => $"#{OrderId}";
        public string CustomerName => _details?.CustomerName ?? "...";
        public string Status => _details?.Status ?? "...";
        public string TrackingNumber => _details?.TrackingNumber;
        public string Address => _details?.Address ?? "...";
        public bool HasPaymentSlip => _details?.PaymentSlipBytes != null && _details.PaymentSlipBytes.Length > 0;
        public string TotalAmountText => $"{_details?.TotalAmount:N0} THB";
        public ObservableCollection<OrderItemViewModel> Items { get; } = new ObservableCollection<OrderItemViewModel>();
        // -----------------------------

        // นี่คือ Constructor ที่ UserMainWindow.xaml.cs เรียกใช้
        public OrderDetailsView(string orderId)
        {
            InitializeComponent();
            DataContext = this;
            _orderId = orderId;
        }

        public async Task LoadOrderDetailsAsync()
        {
            try
            {
                _details = await OrderService.Instance.GetOrderDetailsAsync(_orderId);
                if (_details == null)
                {
                    MessageBox.Show("Order not found.");
                    BackRequested?.Invoke();
                    return;
                }

                // อัปเดต UI
                Raise(nameof(OrderId));
                Raise(nameof(DisplayId));
                Raise(nameof(CustomerName));
                Raise(nameof(Status));
                Raise(nameof(TrackingNumber));
                Raise(nameof(Address));
                Raise(nameof(HasPaymentSlip));
                Raise(nameof(TotalAmountText));

                // โหลดรายการสินค้า
                Items.Clear();
                int index = 1;
                foreach (var item in _details.Items)
                {
                    Items.Add(new OrderItemViewModel
                    {
                        Index = index++,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Total = item.Total
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load order details: " + ex.Message);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke();
        }

        private void PaymentSlip_Click(object sender, RoutedEventArgs e)
        {
            if (!HasPaymentSlip)
            {
                MessageBox.Show("No payment slip was uploaded for this order.");
                return;
            }

            try
            {
                // บันทึกไฟล์สลิปชั่วคราวแล้วเปิด
                string tempPath = Path.Combine(Path.GetTempPath(), $"payment_{_orderId}.png"); // สมมติเป็น PNG
                File.WriteAllBytes(tempPath, _details.PaymentSlipBytes);

                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open payment slip: " + ex.Message);
            }
        }

        private void PdfReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (_details == null) return;

            try
            {
                // --- สร้าง CartLines จำลองสำหรับ ReceiptPdfMaker ---
                var dummyCartLines = _details.Items.Select(item =>
                {
                    var p = new Product { ProductName = item.ProductName, Price = item.Price };
                    var line = new CartLine(p, item.Quantity);

                    // Hack: ตั้งค่าราคาใน Product ให้ตรงกับ Total / Qty
                    p.Price = item.Total / Math.Max(1, item.Quantity); // ตั้งราคาปลอมเพื่อให้ LineTotal ถูก

                    return line;
                }).ToList();

                var profileProvider = new SessionProfileProvider();

                var pdfPath = ReceiptPdfMaker.Create(
                    _orderId,
                    dummyCartLines,
                    _details.TotalAmount,
                    profileProvider,
                    _details.Address
                );

                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to generate PDF receipt: " + ex.Message);
            }
        }

        // คลาสเล็กๆ นี้จำเป็นสำหรับ ReceiptPdfMaker
        private class SessionProfileProvider : IUserProfileProvider
        {
            public int CurrentUserId => Session.CurrentUser?.Id ?? 0;
            public string FullName(int userId) => $"{Session.CurrentUser?.First_Name} {Session.CurrentUser?.Last_Name}".Trim();
            public string Username(int userId) => Session.CurrentUser?.Username ?? "";
            public string Phone(int userId) => Session.CurrentUser?.Phone ?? Session.CurrentUser?.Email ?? "";
        }
    }
}