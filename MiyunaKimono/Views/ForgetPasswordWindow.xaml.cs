using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MiyunaKimono.Services;
using System.Windows.Input; // สำหรับ Mouse.OverrideCursor


namespace MiyunaKimono.Views
{
    public partial class ForgetPasswordWindow : Window
    {
        // สไลด์ภาพ
        private readonly DispatcherTimer _slideTimer = new DispatcherTimer();
        private readonly string[] _slides =
        {
            "pack://application:,,,/Assets/slide1.jpg",
            "pack://application:,,,/Assets/slide2.jpg",
            "pack://application:,,,/Assets/slide3.jpg"
        };
        private int _slideIndex = 0;

        // ส่ง OTP
        private readonly DispatcherTimer _otpTimer = new DispatcherTimer();
        private int _cooldown = 0; // วินาที

        private readonly AuthService _auth = new AuthService();
        private readonly OtpService _otp = new OtpService();

        public ForgetPasswordWindow()
        {
            InitializeComponent();

            // slide
            TrySetSlide(0);
            _slideTimer.Interval = TimeSpan.FromSeconds(4);
            _slideTimer.Tick += (s, e) => { _slideIndex = (_slideIndex + 1) % _slides.Length; TrySetSlide(_slideIndex); };
            _slideTimer.Start();

            // countdown resend
            _otpTimer.Interval = TimeSpan.FromSeconds(1);
            _otpTimer.Tick += (s, e) =>
            {
                _cooldown--;
                if (_cooldown <= 0)
                {
                    _otpTimer.Stop();
                    BtnSend.IsEnabled = true;
                    BtnSend.Content = "Send";
                    LblCountdown.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BtnSend.IsEnabled = false;
                    BtnSend.Content = "Resend";
                    LblCountdown.Visibility = Visibility.Visible;
                    LblCountdown.Text = $"{_cooldown}s";
                }
            };
        }

        private void TrySetSlide(int i)
        {
            try { SlideImage.Source = new BitmapImage(new Uri(_slides[i])); } catch { }
        }

        // ---------- UI actions ----------
        private void Back_Click(object sender, RoutedEventArgs e) => Close();

        private void GoRegister_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var reg = new RegisterWindow { Owner = this.Owner, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            reg.Closed += (s, _) => this.Show();
            reg.Show();
        }

        private void OpenInstagram(object s, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.instagram.com/nlp_xenosz/", UseShellExecute = true });
        private void OpenFacebook(object s, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.facebook.com/thananon.thumaud/", UseShellExecute = true });
        private void OpenX(object s, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://x.com/Xenosz2004?t=6Spmv7eiWTPyNvFwdmWHwg&s=09", UseShellExecute = true });

        // ---------- Validation ----------
        private static bool IsValidEmail(string email)
            => Regex.IsMatch(email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static bool IsValidPassword(string pass)
            => Regex.IsMatch(pass ?? "", @"^(?=.*[A-Za-z])(?=.*\d).{6,}$"); // >=6 มีตัวเลข+ตัวอักษร

        // ---------- OTP ----------
        private async void SendOtp_Click(object sender, RoutedEventArgs e)
        {
            var email = (TxtEmail.Text ?? "").Trim();

            if (!IsValidEmail(email))
            {
                MessageBox.Show("กรุณากรอกอีเมลให้ถูกต้อง", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEmail.Focus();
                return;
            }

            // มีบัญชีนี้ในระบบหรือไม่
            if (!_auth.ExistsEmail(email))
            {
                MessageBox.Show("ไม่พบบัญชีที่ใช้อีเมลนี้", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnSend.IsEnabled = false;                 // กันคลิกซ้ำ
            Mouse.OverrideCursor = Cursors.Wait;       // แสดงเมาส์กำลังทำงาน

            try
            {
                // *** สำคัญ: ใช้ await จริง เพื่อไม่บล็อก UI ***
                var (ok, err) = await _otp.SendOtpAsync(email);

                if (!ok)
                {
                    // ยังไม่ครบ 60 วิ หรือสาเหตุอื่น
                    MessageBox.Show(err, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // แจ้งเตือนว่าจัดส่งแล้ว
                MessageBox.Show($"ส่ง OTP ไปที่ {email} แล้ว กรุณาเช็คอีเมลภายใน 10 นาที",
                                "OTP Sent", MessageBoxButton.OK, MessageBoxImage.Information);

                // เริ่มนับถอยหลัง 60 วิ เพื่อ Resend
                _cooldown = 60;
                LblCountdown.Visibility = Visibility.Visible;
                LblCountdown.Text = "60s";
                BtnSend.Content = "Resend";
                _otpTimer.Start();
            }
            catch (Exception ex)
            {
                // ถ้าส่งเมลล้มเหลว จะมาที่นี่ (แสดงข้อความจริงเพื่อดีบัก)
                MessageBox.Show("ส่ง OTP ไม่สำเร็จ: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // อนุญาตให้กดใหม่ได้
                BtnSend.IsEnabled = true;
                LblCountdown.Visibility = Visibility.Collapsed;
                BtnSend.Content = "Send";
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // ---------- Confirm reset ----------
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var email = (TxtEmail.Text ?? "").Trim();
            var otp = (TxtOtp.Text ?? "").Trim();
            var p1 = TxtNewPass.Text;
            var p2 = TxtNewPass2.Text;

            if (!IsValidEmail(email)) { MessageBox.Show("อีเมลไม่ถูกต้อง", "Warning"); return; }
            if (string.IsNullOrWhiteSpace(otp)) { MessageBox.Show("กรุณากรอก OTP", "Warning"); return; }
            if (!IsValidPassword(p1)) { MessageBox.Show("รหัสผ่านต้องอย่างน้อย 6 ตัวและมีทั้งตัวเลขและตัวอักษร", "Warning"); return; }
            if (p1 != p2) { MessageBox.Show("รหัสผ่านใหม่ทั้งสองช่องต้องเหมือนกัน", "Warning"); return; }

            try
            {
                if (!_otp.VerifyOtpAndConsume(email, otp))
                {
                    MessageBox.Show("OTP ไม่ถูกต้องหรือหมดอายุ", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_auth.IsSamePassword(email, p1))
                {
                    MessageBox.Show("ไม่สามารถใช้รหัสผ่านเดิมได้", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_auth.ResetPassword(email, p1))
                {
                    MessageBox.Show("รีเซ็ตรหัสผ่านไม่สำเร็จ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show("รีเซ็ตรหัสผ่านสำเร็จ กรุณา Log in ใหม่", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                Close(); // กลับไปหน้า Log in
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาด: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
