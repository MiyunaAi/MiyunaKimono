using MiyunaKimono.Models;
using MiyunaKimono.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MiyunaKimono.Views
{
    public partial class ProductListWindow : Window, INotifyPropertyChanged
    {
        private readonly ProductService _service = new();

        public string CategoryFilter { get; }
        public string PageTitle { get; }

        public ObservableCollection<TopPickItem> AllProducts { get; } = new();
        public int AllProductsCount => AllProducts.Count;

        public ProductListWindow(string pageTitle, string categoryFilter = null)
        {
            InitializeComponent();
            DataContext = this;

            PageTitle = pageTitle;
            CategoryFilter = categoryFilter;

            Loaded += async (_, __) =>
            {
                try
                {
                    // ดึงทั้งหมดแล้วค่อยกรอง
                    var list = _service.GetAll();

                    if (!string.IsNullOrWhiteSpace(CategoryFilter))
                    {
                        list = list
                            .Where(p => string.Equals(p.Category ?? "",
                                                      CategoryFilter,
                                                      StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    AllProducts.Clear();
                    foreach (var p in list)
                    {
                        // คำนวณข้อความส่วนลด (ถ้าตารางเก็บเป็น % ก็แปลงตามจริง)
                        var discPercent = (int)Math.Round(p.Discount, MidpointRounding.AwayFromZero);
                        var offText = discPercent > 0 ? $"{discPercent}% OFF" : null;

                        AllProducts.Add(new TopPickItem
                        {
                            ProductName = p.ProductName,
                            Category = p.Category,
                            Price = p.Price,            // TopPickItem.Price เป็น decimal แล้ว
                            Quantity = p.Quantity,
                            Image1Path = p.Image1Path,
                            OffText = offText
                        });
                    }

                    OnPropertyChanged(nameof(AllProductsCount));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Load products failed: " + ex.Message);
                }
            };
        }

        private void Heart_Checked(object sender, RoutedEventArgs e)
        {
            var dc = (sender as FrameworkElement)?.DataContext;
            if (dc is MiyunaKimono.Models.TopPickItem t)
                FavoritesService.Instance.Set(t.Id, true);
            else if (dc is MiyunaKimono.Models.Product p)
                FavoritesService.Instance.Set(p.Id, true);
        }

        private void Heart_Unchecked(object sender, RoutedEventArgs e)
        {
            var dc = (sender as FrameworkElement)?.DataContext;
            if (dc is MiyunaKimono.Models.TopPickItem t)
                FavoritesService.Instance.Set(t.Id, false);
            else if (dc is MiyunaKimono.Models.Product p)
                FavoritesService.Instance.Set(p.Id, false);
        }


        // ===== Nav ปุ่มด้านบน =====
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            new UserMainWindow().Show();
            Close();
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            // รับโมเดลจาก Tag ของปุ่ม
            var ctx = (sender as FrameworkElement)?.Tag;

            MiyunaKimono.Models.Product product = null;

            if (ctx is MiyunaKimono.Models.Product p)
            {
                product = p;
            }
            else if (ctx is MiyunaKimono.Models.TopPickItem card)
            {
                // หากคุณเก็บแค่การ์ด ให้ดึงของจริงจากบริการ (ปรับตามที่คุณมี)
                // ตัวอย่าง: หาจากชื่อ
                product = _service.GetByName(card.ProductName);

            }

            if (product == null)
            {
                MessageBox.Show("ไม่พบข้อมูลสินค้า");
                return;
            }

            var w = new ProductDetailsWindow(product);
            w.Owner = this;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.ShowDialog();
        }


        private void AllKimono_Click(object sender, RoutedEventArgs e)
        {
            new ProductListWindow("All Product", null).Show();
            Close();
        }

        private void Furisode_Click(object sender, RoutedEventArgs e)
        {
            new ProductListWindow("Furisode Kimono", "Furisode").Show();
            Close();
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
