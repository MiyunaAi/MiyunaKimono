using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MiyunaKimono.Models;
using MiyunaKimono.Services;
using System.Linq; // 

namespace MiyunaKimono.Views.Parts
{
    public partial class ProductView : UserControl
    {
        private readonly ProductService _svc = new ProductService();

        // 1. เพิ่ม List สำหรับเก็บข้อมูลต้นฉบับ
        private List<Product> _allProducts;

        public ProductView()
        {
            InitializeComponent();
            // 
            Loaded += (_, __) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                // 2. ดึงข้อมูลทั้งหมดเก็บใน List ต้นฉบับ
                _allProducts = _svc.GetAll();
                GridProducts.ItemsSource = _allProducts;
            }
            catch (Exception ex)
            {
                MessageBox.Show("โหลดข้อมูลสินค้าไม่สำเร็จ:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 3. 
        // (เพื่อให้ AdminWindow เรียกใช้ได้)
        public void FilterProducts(string searchText)
        {
            if (_allProducts == null) return;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // ถ้าช่องค้นหาว่าง ให้แสดงสินค้าทั้งหมด
                GridProducts.ItemsSource = _allProducts;
            }
            else
            {
                // ถ้ามีคำค้นหา
                var lowerSearch = searchText.ToLowerInvariant();

                // กรองข้อมูลจาก List ต้นฉบับ
                var filtered = _allProducts.Where(p =>
                    (p.ProductName != null && p.ProductName.ToLowerInvariant().Contains(lowerSearch)) ||
                    (p.ProductCode != null && p.ProductCode.ToLowerInvariant().Contains(lowerSearch))
                ).ToList();

                // แสดงผลลัพธ์ที่กรองแล้ว
                GridProducts.ItemsSource = filtered;
            }
        }
        // 

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            // (โค้ดเดิมของคุณ)
            if ((sender as FrameworkElement)?.DataContext is Product p)
            {
                if (Window.GetWindow(this) is AdminWindow win)
                {
                    await win.ShowEditProductAsync(p.Id);
                }
            }
        }
    }
}