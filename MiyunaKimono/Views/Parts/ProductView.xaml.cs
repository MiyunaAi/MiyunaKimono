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
using System.Collections.Generic;
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
            LoadData();
        }

        private void LoadData()
        {
            List<Product> items = _svc.GetAll();
            GridProducts.ItemsSource = items;
        }
    }
}
