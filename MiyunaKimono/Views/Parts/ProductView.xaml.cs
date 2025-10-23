using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MiyunaKimono.Models;
using MiyunaKimono.Services;

namespace MiyunaKimono.Views.Parts
{
    public partial class ProductView : UserControl
    {
        private readonly ProductService _svc = new ProductService();

        public ProductView()
        {
            InitializeComponent();
            // ให้ดึงข้อมูลหลัง UI พร้อมแล้ว ป้องกันกรณีชื่อคอนโทรลยังไม่ถูกสร้าง
            Loaded += (_, __) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                List<Product> items = _svc.GetAll();
                GridProducts.ItemsSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show("โหลดข้อมูลสินค้าไม่สำเร็จ:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            // ดึง Product จาก DataContext ของปุ่ม Edit แถวที่กด
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
