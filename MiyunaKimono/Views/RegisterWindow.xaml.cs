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
using MiyunaKimono.Models;
using MiyunaKimono.Services;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text.RegularExpressions;


namespace MiyunaKimono.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly DispatcherTimer _slideTimer = new DispatcherTimer();
        private readonly string[] _slides;
        private int _slideIndex = 0;

        private readonly AuthService _auth = new AuthService(); // ปรับตามโปรเจกต์คุณ

        public RegisterWindow()
        {
            InitializeComponent();

            // slide images (เปลี่ยน path ตามรูปของคุณ)
            _slides = new[]
            {
                "pack://application:,,,/Assets/slide1.jpg",
                "pack://application:,,,/Assets/slide2.jpg",
                "pack://application:,,,/Assets/slide3.jpg"
            };

            try
            {
                SlideImage.Source = new BitmapImage(new Uri(_slides[0]));
            }
            catch { /* ถ้ารูปหาย ให้ข้ามไป */ }

            _slideTimer.Interval = TimeSpan.FromSeconds(4);
            _slideTimer.Tick += (s, e) => {
                _slideIndex = (_slideIndex + 1) % _slides.Length;
                try { SlideImage.Source = new BitmapImage(new Uri(_slides[_slideIndex])); } catch { }
            };
            _slideTimer.Start();
        }

        private void GoLogin_Click(object sender, RoutedEventArgs e)
        {
            // ปิด Register แล้วกลับไป Login
            Close();
        }

        private static bool IsValidEmail(string email)
    => Regex.IsMatch(email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private static bool IsValidPhone(string phone)
            => Regex.IsMatch(phone ?? "", @"^\+?\d{9,15}$"); // + ตามด้วย 9–15 หลัก

        private static bool IsValidUsername(string user)
            => Regex.IsMatch(user ?? "", @"^[A-Za-z0-9_]{6,}$"); // อย่างน้อย 6 ตัว

        private static bool IsValidPassword(string pass)
            => Regex.IsMatch(pass ?? "", @"^(?=.*[A-Za-z])(?=.*\d).{6,}$"); // มีตัวอักษร+ตัวเลข ≥ 6 ตัว


        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            var first = TxtFirst.Text.Trim();
            var last = TxtLast.Text.Trim();
            var email = TxtEmail.Text.Trim();
            var phone = TxtPhone.Text.Trim();
            var user = TxtUser.Text.Trim();
            var pass1 = TxtPass.Text;
            var pass2 = TxtPass2.Text;

            // ตรวจว่ากรอกครบ
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass1) ||
                string.IsNullOrWhiteSpace(pass2))
            {
                MessageBox.Show("กรุณากรอกข้อมูลให้ครบทุกช่อง", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ✅ ตรวจรูปแบบตามเงื่อนไข
            if (!IsValidEmail(email))
            {
                MessageBox.Show("รูปแบบอีเมลไม่ถูกต้อง", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEmail.Focus();
                return;
            }

            if (!IsValidPhone(phone))
            {
                MessageBox.Show("รูปแบบเบอร์โทรไม่ถูกต้อง (ใส่เฉพาะตัวเลข หรือขึ้นต้นด้วย + ได้ และต้องยาว 9–15 หลัก)", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPhone.Focus();
                return;
            }

            if (!IsValidUsername(user))
            {
                MessageBox.Show("Username ต้องอย่างน้อย 6 ตัว และใช้ได้เฉพาะ a-z A-Z 0-9 และ _", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUser.Focus();
                return;
            }

            if (!IsValidPassword(pass1))
            {
                MessageBox.Show("Password ต้องอย่างน้อย 6 ตัว และมีทั้งตัวอักษรและตัวเลข", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPass.Focus();
                return;
            }

            if (pass1 != pass2)
            {
                MessageBox.Show("Password และ Confirm Password ไม่ตรงกัน", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPass2.Focus();
                return;
            }

            // ----- จากตรงนี้ค่อยทำเช็คซ้ำในฐานข้อมูล / บันทึก -----
            try
            {
                if (_auth.ExistsEmail(email)) { MessageBox.Show("อีเมลนี้มีผู้ใช้แล้ว", "Warning"); return; }
                if (_auth.ExistsPhone(phone)) { MessageBox.Show("เบอร์โทรนี้มีผู้ใช้แล้ว", "Warning"); return; }
                if (_auth.ExistsUsername(user)) { MessageBox.Show("Username นี้มีผู้ใช้แล้ว", "Warning"); return; }

                var ok = _auth.Register(first, last, email, phone, user, pass1);
                if (!ok)
                {
                    MessageBox.Show("สมัครไม่สำเร็จ ลองใหม่อีกครั้ง", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show("สมัครบัญชีแล้ว กรุณา login", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // ถ้าเปิด Register ด้วย ShowDialog() ให้ใช้บรรทัดนี้
                // this.DialogResult = true;

                this.Close(); // กลับไปหน้า Log in (LoginWindow จะ Show ตามที่คุณผูกไว้)
            }
            catch (Exception ex)
            {
                MessageBox.Show("เกิดข้อผิดพลาด: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}