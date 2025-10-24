// Views/LoginWindow.xaml.cs
using MiyunaKimono.Services;
using System;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace MiyunaKimono.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _auth = new();
        private readonly DispatcherTimer _slideTimer = new();
        private readonly string[] _slides;

        private bool _showingPlain = false;
        private bool _isShowingPassword = false;   // state เปิด/ปิดตา
        private string _plainPasswordCache = "";

        public LoginWindow()
        {
            InitializeComponent();

            // โหลดรูปสไลด์ 3 รูปใน Assets
            _slides = new[] { "slide1.jpg", "slide2.jpg", "slide3.jpg" };
            for (int i = 0; i < _slides.Length; i++)
                _slides[i] = $"pack://application:,,,/Assets/{_slides[i]}";

            SlideImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(_slides[0]));
            _slideTimer.Interval = TimeSpan.FromSeconds(4);
            _slideTimer.Tick += (s, e) => NextSlide();
            _slideTimer.Start();


            // Remember username
            if (Properties.Settings.Default.RememberMe && !string.IsNullOrWhiteSpace(Properties.Settings.Default.SavedUsername))
                TxtUsername.Text = Properties.Settings.Default.SavedUsername;
            PwdBox.PasswordChanged += PwdBox_PasswordChanged;
        }

        private int _slideIndex = 0;
        private void NextSlide()
        {
            _slideIndex = (_slideIndex + 1) % _slides.Length;
            SlideImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(_slides[_slideIndex]));
        }

        private void BtnEye_Click(object sender, RoutedEventArgs e)
        {
            _isShowingPassword = !_isShowingPassword;

            if (_isShowingPassword)
            {
                // โชว์รหัสผ่านใน TextBox แล้วซ่อน PasswordBox
                PwdPlain.Text = PwdBox.Password;
                PwdPlain.Visibility = Visibility.Visible;
                PwdBox.Visibility = Visibility.Collapsed;

                // เปลี่ยนไอคอนเป็น open
                EyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/Assets/eye_open.png"));
            }
            else
            {
                // กลับไปซ่อน: sync ค่ากลับไปที่ PasswordBox
                PwdBox.Password = PwdPlain.Text;
                PwdPlain.Visibility = Visibility.Collapsed;
                PwdBox.Visibility = Visibility.Visible;

                // เปลี่ยนไอคอนเป็น closed
                EyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/Assets/eye_closed.png"));
            }
        }

        // ---------- (แนะนำ) sync แบบเรียลไทม์ตอนพิมพ์ ----------
        private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isShowingPassword)
                PwdPlain.Text = PwdBox.Password;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var user = TxtUsername.Text.Trim();
            var pass = PwdBox.Password;

            if (user == "Ishihara" && pass == "wasd5247")
            {
                new MiyunaKimono.Views.AdminWindow().Show();
                this.Close();
                return;
            }


            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("กรอก Username และ Password", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool ok = false;
            try { ok = _auth.Login(user, pass); }
            catch (Exception ex)
            {
                MessageBox.Show("เชื่อมฐานข้อมูลไม่สำเร็จ: " + ex.Message);
                return;
            }

            if (!ok)
            {
                MessageBox.Show("ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง", "Login failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // บันทึก Remember Me
            Properties.Settings.Default.RememberMe = ChkRemember.IsChecked == true;
            Properties.Settings.Default.SavedUsername = (ChkRemember.IsChecked == true) ? user : "";
            Properties.Settings.Default.Save();

            // ★ สำคัญ: ถ้ายังไม่มี CurrentUserId ใน AuthService ให้ใช้เมธอด GetUserId แทน
            int userId = _auth.GetUserIdByUsername(user); // เมธอดนี้คุณจะเพิ่มใน AuthService (ข้อ 2)
            if (userId > 0)
            {
                FavoritesService.Instance.InitForUser(userId);
            }

            MessageBox.Show("เข้าสู่ระบบสำเร็จ!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            new UserMainWindow().Show();
            Close();
        }


        private void GoRegister_Click(object sender, RoutedEventArgs e)
        {
            Register_Click(sender, e); // เรียกเมธอดหลักตัวเดียว
        }

        // ใช้เมธอดเดียวกันกับลิงก์ Register here
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();

            var reg = new RegisterWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            // เมื่อปิด Register -> โชว์ Login กลับ
            reg.Closed += (s, _) => this.Show();

            reg.Show();   // ✅ เปิดแบบ non-modal
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new ForgetPasswordWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            win.Closed += (s, _) => this.Show();
            win.Show(); // หรือ ShowDialog() ก็ได้ถ้าต้องการ flow แบบ modal
        }


        private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        private void OpenInstagram(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.instagram.com/nlp_xenosz/",
                UseShellExecute = true
            });
        }
        private void OpenFacebook(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.facebook.com/thananon.thumaud/",
                UseShellExecute = true
            });
        }
        private void OpenX(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://x.com/Xenosz2004?t=6Spmv7eiWTPyNvFwdmWHwg&s=09",
                UseShellExecute = true
            });
        }

    }
}
