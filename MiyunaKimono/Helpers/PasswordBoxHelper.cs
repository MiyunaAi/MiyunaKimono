using System.Windows;
using System.Windows.Controls;

namespace MiyunaKimono.Helpers
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty TrackProperty =
            DependencyProperty.RegisterAttached(
                "Track", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnTrackChanged));

        public static void SetTrack(DependencyObject d, bool v) => d.SetValue(TrackProperty, v);
        public static bool GetTrack(DependencyObject d) => (bool)d.GetValue(TrackProperty);

        public static readonly DependencyProperty LengthProperty =
            DependencyProperty.RegisterAttached(
                "Length", typeof(int), typeof(PasswordBoxHelper),
                new PropertyMetadata(0));

        public static void SetLength(DependencyObject d, int v) => d.SetValue(LengthProperty, v);
        public static int GetLength(DependencyObject d) => (int)d.GetValue(LengthProperty);

        private static void OnTrackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox pb)
            {
                if ((bool)e.NewValue)
                {
                    pb.PasswordChanged -= Pb_PasswordChanged;
                    pb.PasswordChanged += Pb_PasswordChanged;
                    SetLength(pb, pb.Password?.Length ?? 0);
                }
                else
                {
                    pb.PasswordChanged -= Pb_PasswordChanged;
                }
            }
        }

        private static void Pb_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
                SetLength(pb, pb.Password?.Length ?? 0);
        }
    }
}
