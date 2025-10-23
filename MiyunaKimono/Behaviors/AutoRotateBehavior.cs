// Behaviors/AutoRotateBehavior.cs
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

        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

        public static readonly DependencyProperty IntervalSecondsProperty =
            DependencyProperty.RegisterAttached(
                "IntervalSeconds", typeof(int), typeof(AutoRotateBehavior),
                new PropertyMetadata(4, OnIntervalChanged));

        public static void SetIntervalSeconds(DependencyObject obj, int value) => obj.SetValue(IntervalSecondsProperty, value);
        public static int GetIntervalSeconds(DependencyObject obj) => (int)obj.GetValue(IntervalSecondsProperty);

        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached("Timer", typeof(DispatcherTimer), typeof(AutoRotateBehavior));

        private static void SetTimer(DependencyObject obj, DispatcherTimer t) => obj.SetValue(TimerProperty, t);
        private static DispatcherTimer GetTimer(DependencyObject obj) => (DispatcherTimer)obj.GetValue(TimerProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox lb) return;

            if ((bool)e.NewValue)
            {
                var timer = GetTimer(lb) ?? new DispatcherTimer();
                timer.Tick += (_, __) => Rotate(lb);
                timer.Interval = TimeSpan.FromSeconds(Math.Max(1, GetIntervalSeconds(lb)));
                timer.Start();
                SetTimer(lb, timer);

                lb.Unloaded += Lb_Unloaded;
            }
            else
            {
                Stop(lb);
            }
        }

        private static void OnIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox lb) return;
            var timer = GetTimer(lb);
            if (timer != null && GetIsEnabled(lb))
                timer.Interval = TimeSpan.FromSeconds(Math.Max(1, GetIntervalSeconds(lb)));
        }

        private static void Rotate(ListBox lb)
        {
            int n = lb.Items?.Count ?? 0;
            if (n <= 1) return;
            lb.SelectedIndex = (lb.SelectedIndex + 1) % n;
        }

        private static void Lb_Unloaded(object s, RoutedEventArgs e)
        {
            if (s is ListBox lb) Stop(lb);
        }

        private static void Stop(ListBox lb)
        {
            var timer = GetTimer(lb);
            if (timer != null)
            {
                timer.Stop();
                SetTimer(lb, null);
            }
            lb.Unloaded -= Lb_Unloaded;
        }
    }
}
