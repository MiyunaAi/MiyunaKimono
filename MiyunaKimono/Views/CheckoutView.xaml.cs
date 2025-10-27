using Microsoft.Win32;
using MiyunaKimono.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MiyunaKimono.Views
{
    public partial class CheckoutView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ---- Bind เหมือน CartView ----
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

        // ---- QR ----
        private readonly DispatcherTimer _qrTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private int _qrRemain = 59;
        public string QrRemainText => _qrRemain.ToString();

        private byte[] _receiptBytes;   // เก็บไฟล์สลิป
        private string _receiptPath;    // แสดงในกล่อง

        public CheckoutView()
        {
            InitializeComponent();
            DataContext = this;

            // ทำ QR แรก
            MakeQr();

            _qrTimer.Tick += (_, __) =>
            {
                if (_qrRemain > 0)
                {
                    _qrRemain--;
                    PropertyChanged?.Invoke(this, new(nameof(QrRemainText)));
                    if (_qrRemain == 0) BtnResetQr.Visibility = Visibility.Visible;
                }
            };
            _qrTimer.Start();
        }

        private void MakeQr()
        {
            // โทรศัพท์ PromptPay (เบอร์โทร 10 หลัก)
            const string phone = "0800316386";
            // ยอดรวม
            var amount = GrandTotal;

            // สร้าง payload EMVCo + PromptPay (โค้ดง่าย ๆ พอใช้งาน)
            var payload = PromptPayQr.BuildMobilePayload(phone, amount);

            // สร้างภาพ QR ด้วย QRCoder (ติดตั้ง NuGet: QRCoder)
            // PM> Install-Package QRCoder
            var generator = new QRCoder.QRCodeGenerator();
            var data = generator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.M);
            var code = new QRCoder.PngByteQRCode(data);
            var bytes = code.GetGraphic(7);

            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            QrImage.Source = bmp;

            // รีเซตเวลา
            _qrRemain = 59;
            BtnResetQr.Visibility = Visibility.Collapsed;
            PropertyChanged?.Invoke(this, new(nameof(QrRemainText)));
        }

        private void ResetQr_Click(object sender, RoutedEventArgs e) => MakeQr();

        private void UploadReceipt_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select payment receipt",
                Filter = "Images/PDF|*.png;*.jpg;*.jpeg;*.pdf",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                _receiptPath = dlg.FileName;
                ReceiptPathBox.Text = _receiptPath;
                _receiptBytes = File.ReadAllBytes(_receiptPath);
            }
        }

        // แจ้งให้ parent (UserMainWindow) ไปหน้า Home
        public event Action BackRequested;

        // ★ เพิ่มอีเวนต์แจ้งว่าออเดอร์สำเร็จแล้ว (ให้ UserMainWindow รีโหลดสินค้า)
        public event Action OrderCompleted;

        private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();

        private async void Checkout_Click(object sender, RoutedEventArgs e)
        {
            if (_receiptBytes == null || _receiptBytes.Length == 0)
            {
                MessageBox.Show("กรุณาอัปโหลดสลิปก่อน", "Upload required",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (Lines.Count == 0)
            {
                MessageBox.Show("ตะกร้าว่าง", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // ดึงข้อมูลผู้ใช้/ที่อยู่ล่าสุดจาก cart (บันทึกไว้ใน CartView ตอนกด Checkout ของ Cart)
                var userId = AuthService.CurrentUserIdSafe();
                var addr = CartPersistenceService.Instance.LastAddressForOrder ?? "";

                var u = Session.CurrentUser; // มี FirstName, LastName, Username, Email
                var fullName = $"{u?.FirstName} {u?.LastName}".Trim();
                var username = u?.Username ?? "";
                var telOrEmail = u?.Email ?? ""; // ถ้าไม่มีเบอร์โทร ใช้อีเมลแทนชั่วคราว

                var orderId = await OrderService.Instance.CreateOrderFullAsync(
                    userId: userId,
                    customerFullName: fullName,
                    username: username,
                    address: addr,
                    tel: telOrEmail,
                    lines: Lines.ToList(),
                    total: GrandTotal,
                    discount: DiscountTotal,
                    receiptBytes: _receiptBytes,
                    receiptFileName: System.IO.Path.GetFileName(_receiptPath)
                );

                // ออกรายงาน PDF
                var pdfPath = ReceiptPdfMaker.Create(
                    orderId,
                    Lines.ToList(),
                    GrandTotal,
                    new SessionProfileProvider(),   // <- โปรไวเดอร์เล็ก ๆ ด้านล่าง
                    addr
                );
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });


                // เคลียร์ตะกร้า + บันทึกสถานะล่าสุด
                CartService.Instance.Clear();
                CartPersistenceService.Instance.Save(userId, Lines.ToList()); // จะว่าง

                MessageBox.Show("ทำรายการสั่งซื้อสำเร็จ โปรดตรวจสอบสถานะสินค้า", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                // ★ แจ้ง UserMainWindow ให้รีโหลดข้อมูลสินค้า/สต็อกใหม่
                OrderCompleted?.Invoke();

                BackRequested?.Invoke(); // กลับ Home
            }
            catch (Exception ex)
            {
                MessageBox.Show("ทำรายการไม่สำเร็จ: " + ex.Message);
            }
        }
    }
    internal sealed class SessionProfileProvider : IUserProfileProvider
    {
        public int CurrentUserId => AuthService.CurrentUserIdSafe();

        public string FullName(int userId)
        {
            var u = Session.CurrentUser;
            return $"{u?.FirstName} {u?.LastName}".Trim();
        }

        public string Username(int userId)
            => Session.CurrentUser?.Username ?? "";

        public string Phone(int userId)
            => Session.CurrentUser?.Email ?? ""; // ถ้าไม่มีเบอร์โทรจริง ใช้อีเมลแทนชั่วคราว
    }

    // ===== PromptPay EMVCo payload แบบย่อ =====
    internal static class PromptPayQr
    {
        // อ้างอิงหลักการ EMVCo; โค้ดนี้ทำ payload ได้พอใช้งานจริง (มือถือ + จำนวนเงิน)
        public static string BuildMobilePayload(string mobile10, decimal amount)
        {
            // ตัด 0 นำหน้า แล้วแปลงเป็น +66
            var mobile = mobile10.Trim();
            if (mobile.StartsWith("0")) mobile = "66" + mobile.Substring(1);

            // TLV helper
            string TLV(string id, string value)
                => id + value.Length.ToString("00") + value;

            // Merchant account (PromptPay mobile)
            // AID = A000000677010111
            var merchantInfo = TLV("00", "A000000677010111") + TLV("01", "11" + mobile);
            var tag26 = TLV("26", merchantInfo);

            var amt = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            var payload =
                TLV("00", "01") +           // Payload Format Indicator
                TLV("01", "12") +           // Point of initiation (static=11/dynamic=12) - ใช้ 12
                tag26 +
                TLV("52", "0000") +         // Merchant Category Code (ไม่ระบุ)
                TLV("53", "764") +          // Currency = THB
                TLV("54", amt) +            // Amount
                TLV("58", "TH") +           // Country Code
                TLV("59", "Miyuna") +       // Merchant Name (สั้น ๆ)
                TLV("60", "Bangkok") +      // City
                "6304";                     // CRC placeholder

            // คำนวณ CRC-16/CCITT-FALSE
            var crc = Crc16Ccitt(payload);
            return payload + crc;
        }

        private static string Crc16Ccitt(string s)
        {
            ushort poly = 0x1021, reg = 0xFFFF;
            var bytes = System.Text.Encoding.ASCII.GetBytes(s);
            foreach (var b in bytes)
            {
                reg ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    reg = (reg & 0x8000) != 0 ? (ushort)((reg << 1) ^ poly) : (ushort)(reg << 1);
                }
            }
            return reg.ToString("X4");
        }
    }
}
