using Microsoft.Win32;
using MiyunaKimono.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;   
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MiyunaKimono.Helpers;
namespace MiyunaKimono.Views
{
    public partial class EditProfileView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void Raise([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // ต้นฉบับ (ไว้ตรวจว่ามีการเปลี่ยนจริง)
        private string _origFirst, _origLast, _origEmail, _origPhone;

        // ค่าแก้ไข
        private string _firstName, _lastName, _email, _phone;
        private byte[] _avatarBytes;              // null = ไม่เปลี่ยน
        private BitmapImage _avatarPreview;

        public string FirstName { get => _firstName; set { _firstName = value; Validate(); Raise(); } }
        public string LastName { get => _lastName; set { _lastName = value; Validate(); Raise(); } }
        public string Email { get => _email; set { _email = value; Validate(); Raise(); } }
        public string Phone { get => _phone; set { _phone = value; Validate(); Raise(); } }
        public BitmapImage AvatarPreview
        {
            get => _avatarPreview;
            private set { _avatarPreview = value; Raise(); }
        }

        public string EmailError { get; private set; }
        public string PhoneError { get; private set; }
        public bool CanSave { get; private set; }

        public event Action BackRequested;
        public event Action Saved; // แจ้ง parent ให้รีโหลด UserInfo

        public EditProfileView()
        {
            InitializeComponent();
            DataContext = this;
            LoadFromSession();

            // --- 🔽 START FIX 1.1 🔽 ---
            // ให้หน้านี้คอยฟังการเปลี่ยนแปลงจาก Session ด้วย
            Session.ProfileChanged += OnProfileChanged;
            this.Unloaded += (s, e) => Session.ProfileChanged -= OnProfileChanged;
            // --- 🔼 END FIX 1.1 🔼 ---
        }

        // --- 🔽 START FIX 1.2 🔽 ---
        // เพิ่มเมธอดนี้ เมื่อ Session แจ้งว่าข้อมูลเปลี่ยน ให้โหลดใหม่
        private void OnProfileChanged()
        {
            Dispatcher.Invoke(LoadFromSession);
        }
        // --- 🔼 END FIX 1.2 🔼 ---

        private void LoadFromSession()
        {
            // ... (โค้ดในเมธอดนี้เหมือนเดิม) ...
            // ...
            var u = Session.CurrentUser;

            _origFirst = u?.First_Name ?? "";
            _origLast = u?.Last_Name ?? "";
            _origEmail = u?.Email ?? "";
            _origPhone = u?.Phone ?? "";

            FirstName = _origFirst;
            LastName = _origLast;
            Email = _origEmail;
            Phone = _origPhone;

            if (!string.IsNullOrWhiteSpace(u?.AvatarPath) && File.Exists(u.AvatarPath))
                AvatarPreview = ImageHelper.LoadBitmapNoCache(u.AvatarPath);
            else
                AvatarPreview = CreateBitmapFromPackUri("pack://application:,,,/Assets/ic_user.png");

            _avatarBytes = null;
            Validate();
            // ...
        }

        // ... (โค้ด CreateBitmapFromPackUri, Validate, Back_Click, ChangeAvatar_Click... เหมือนเดิม) ...


        // --- 🔽 START FIX 1.3 (สำคัญ) 🔽 ---
        // แก้ไข Save_Click ให้ "ไม่ต้อง" ยิงอีเวนต์เอง


        // helper สร้าง BitmapImage จาก pack URI อย่างถูกต้อง
        private static BitmapImage CreateBitmapFromPackUri(string packUri)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(packUri, UriKind.Absolute); // ★ ไม่มีพารามิเตอร์ bool
            bmp.EndInit();
            return bmp;
        }

        private void Validate()
        {
            EmailError = IsValidEmail(Email) ? null : "อีเมลไม่ถูกต้อง";
            PhoneError = IsValidPhone(Phone) ? null : "เบอร์ควรเป็นตัวเลข 9–15 หลัก";

            bool valid = string.IsNullOrWhiteSpace(EmailError) &&
                         string.IsNullOrWhiteSpace(PhoneError) &&
                         !string.IsNullOrWhiteSpace(FirstName) &&
                         !string.IsNullOrWhiteSpace(LastName);

            bool changed = _avatarBytes != null ||
                           !Equals(FirstName, _origFirst) ||
                           !Equals(LastName, _origLast) ||
                           !Equals(Email, _origEmail) ||
                           !Equals(Phone, _origPhone);

            CanSave = valid && changed;
            Raise(nameof(EmailError));
            Raise(nameof(PhoneError));
            Raise(nameof(CanSave));
        }

        private static bool IsValidEmail(string s)
            => !string.IsNullOrWhiteSpace(s) &&
               Regex.IsMatch(s, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private static bool IsValidPhone(string s)
            => !string.IsNullOrWhiteSpace(s) &&
               Regex.IsMatch(s, @"^\d{9,15}$");

        private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();

        private void ChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select avatar",
                Filter = "Image files|*.png;*.jpg;*.jpeg",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                _avatarBytes = File.ReadAllBytes(dlg.FileName);

                // พรีวิวจากบัฟเฟอร์ (ยังไม่แตะไฟล์จริง)
                using var ms = new MemoryStream(_avatarBytes);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                AvatarPreview = bmp;

                Validate();
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!CanSave) return;
            try
            {
                int userId = AuthService.CurrentUserIdSafe();

                // 1. เรียก Service (ตัว Service ที่แก้ไปรอบที่แล้ว จะอัปเดต Session
                //    และยิง ProfileChanged เอง ซึ่งเพียงพอแล้ว)
                await UserService.Instance.UpdateProfileAsync(
                    userId, FirstName, LastName, Email, Phone, _avatarBytes);

                // 2. (ลบโค้ด if/else ที่เรียก Session.UpdateAvatarPath/RaiseProfileChanged ซ้ำซ้อนทิ้ง)
                //    อีเวนต์จาก UserService (FIX 1.1) จะอัปเดต AvatarPreview ให้อัตโนมัติ

                // 3. แจ้งเตือนและนำทางกลับ
                MessageBox.Show("Saved.", "Profile", MessageBoxButton.OK, MessageBoxImage.Information);
                Saved?.Invoke();
                BackRequested?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }
        // --- 🔼 END FIX 1.3 🔼 ---


    }
}
