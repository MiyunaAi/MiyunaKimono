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
        }

        private void LoadFromSession()
        {
            // ★ ใช้ Models.User (จาก Services.Session)
            var u = Session.CurrentUser;

            // map ให้ตรงกับโมเดลของคุณ
            _origFirst = u?.First_Name ?? "";
            _origLast = u?.Last_Name ?? "";
            _origEmail = u?.Email ?? "";
            _origPhone = u?.Phone ?? "";

            FirstName = _origFirst;
            LastName = _origLast;
            Email = _origEmail;
            Phone = _origPhone;

            // โหลดภาพเริ่มต้น (ยังไม่รองรับ AvatarPath ในโมเดล → ใช้ default)
            AvatarPreview = CreateBitmapFromPackUri("pack://application:,,,/Assets/ic_user.png");
            _avatarBytes = null;

            Validate();
        }

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
            var dlg = new OpenFileDialog
            {
                Title = "Select avatar",
                Filter = "Image files|*.png;*.jpg;*.jpeg",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                _avatarBytes = File.ReadAllBytes(dlg.FileName);
                using var ms = new MemoryStream(_avatarBytes);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
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
                // เมธอดนี้จะคืน path ใหม่ ถ้ามีบันทึกรูป
                var newPath = await UserService.Instance.UpdateProfileAsync(
                    userId,
                    FirstName, LastName, Email, Phone,
                    _avatarBytes  // null = ไม่เปลี่ยนรูป
                );

                // อัปเดต Session ให้ UI อื่น ๆ เห็นทันที
                if (Session.CurrentUser != null)
                {
                    Session.CurrentUser.First_Name = FirstName;
                    Session.CurrentUser.Last_Name = LastName;
                    Session.CurrentUser.Email = Email;
                    Session.CurrentUser.Phone = Phone;
                    if (!string.IsNullOrWhiteSpace(newPath))
                    {
                        Session.CurrentUser.AvatarPath = newPath;  // ★ เซ็ต path ใหม่
                    }
                }

                // แจ้งทั่วแอปว่ารูป/โปรไฟล์เปลี่ยนแล้ว → Top bar จะรีเฟรชเอง
                Session.RaiseProfileChanged();

                MessageBox.Show("Saved.", "Profile", MessageBoxButton.OK, MessageBoxImage.Information);

                // ยิงอีเวนต์ให้ UserMainWindow กลับหน้า UserInfo (คุณมีโค้ดไว้แล้ว)
                Saved?.Invoke();
                BackRequested?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }


    }
}
