using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Helpers/PasswordBoxHelper.cs
using System.Windows;
using System.Windows.Controls;

namespace MiyunaKimono.Helpers
{
    // ทำ property Length ให้ bind ได้ และเปิด/ปิดการติดตามด้วย Track
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty TrackProperty =
            DependencyProperty.RegisterAttached(
                "Track", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnTrackChanged));

        public static void SetTrack(DependencyObject obj, bool value) => obj.SetValue(TrackProperty, value);
        public static bool GetTrack(DependencyObject obj) => (bool)obj.GetValue(TrackProperty);

        public static readonly DependencyProperty LengthProperty =
            DependencyProperty.RegisterAttached(
                "Length", typeof(int), typeof(PasswordBoxHelper),
                new PropertyMetadata(0));

        public static int GetLength(DependencyObject obj) => (int)obj.GetValue(LengthProperty);
        private static void SetLength(DependencyObject obj, int value) => obj.SetValue(LengthProperty, value);

        private static void OnTrackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pb = d as PasswordBox;
            if (pb == null) return;

            if ((bool)e.NewValue)
            {
                pb.PasswordChanged += (s, ev) => SetLength(pb, pb.Password?.Length ?? 0);
                // set initial
                SetLength(pb, pb.Password?.Length ?? 0);
            }
        }
    }
}

