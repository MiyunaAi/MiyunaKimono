using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MiyunaKimono.Behaviors
{
    public static class AutoRotateBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(AutoRotateBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static void SetIsEnabled(DependencyObject d, bool v) => d.SetValue(IsEnabledProperty, v);
        public static bool GetIsEnabled(DependencyObject d) => (bool)d.GetValue(IsEnabledProperty);

        public static readonly DependencyProperty IntervalSecondsProperty =
            DependencyProperty.RegisterAttached(
                "IntervalSeconds", typeof(int), typeof(AutoRotateBehavior),
                new PropertyMetadata(4, OnIntervalChanged));

        public static void SetIntervalSeconds(DependencyObject d, int v) => d.SetValue(IntervalSecondsProperty, v);
        public static int GetIntervalSeconds(DependencyObject d) => (int)d.GetValue(IntervalSecondsProperty);

        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached("Timer", typeof(DispatcherTimer), typeof(AutoRotateBehavior));

        private static void SetTimer(DependencyObject d, DispatcherTimer v) => d.SetValue(TimerProperty, v);
        private static DispatcherTimer GetTimer(DependencyObject d) => (DispatcherTimer)d.GetValue(TimerProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox list) return;

            if ((bool)e.NewValue)
            {
                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(GetIntervalSeconds(list)) };
                t.Tick += (_, __) =>
                {
                    if (list.Items.Count == 0) return;
                    list.SelectedIndex = (list.SelectedIndex + 1) % list.Items.Count;
                };
                SetTimer(list, t);
                t.Start();
            }
            else
            {
                var t = GetTimer(list);
                t?.Stop();
                SetTimer(list, null);
            }
        }

        private static void OnIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var t = GetTimer(d);
            if (t != null) t.Interval = TimeSpan.FromSeconds(GetIntervalSeconds(d));
        }
    }
}
