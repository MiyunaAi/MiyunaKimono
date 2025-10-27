using System;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows;

namespace MiyunaKimono.Behaviors
{
    public static class AutoRotateBehavior
    {
        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(AutoRotateBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static int GetIntervalSeconds(DependencyObject obj) => (int)obj.GetValue(IntervalSecondsProperty);
        public static void SetIntervalSeconds(DependencyObject obj, int value) => obj.SetValue(IntervalSecondsProperty, value);
        public static readonly DependencyProperty IntervalSecondsProperty =
            DependencyProperty.RegisterAttached("IntervalSeconds", typeof(int), typeof(AutoRotateBehavior),
                new PropertyMetadata(4));

        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached("Timer", typeof(DispatcherTimer), typeof(AutoRotateBehavior));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Selector sel) return;

            // stop old
            if (sel.GetValue(TimerProperty) is DispatcherTimer oldT)
            {
                oldT.Stop();
                sel.ClearValue(TimerProperty);
            }

            if ((bool)e.NewValue)
            {
                var t = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(Math.Max(1, GetIntervalSeconds(sel)))
                };
                t.Tick += (_, __) =>
                {
                    if (sel.Items.Count == 0) return;
                    var next = (sel.SelectedIndex + 1) % sel.Items.Count;
                    sel.SelectedIndex = next;
                };
                sel.Unloaded += (_, __) => t.Stop();
                t.Start();
                sel.SetValue(TimerProperty, t);
            }
        }
    }
}
