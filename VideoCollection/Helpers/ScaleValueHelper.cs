using System;
using System.Windows;
using VideoCollection.CustomTypes;

namespace VideoCollection.Helpers
{
    internal static class ScaleValueHelper
    {
        public static DependencyProperty SetScaleValueProperty<T>() where T : DependencyObject, new()
        {
            return DependencyProperty.Register("ScaleValue", typeof(double), typeof(T), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));
        }

        public static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            if (o != null)
                return OnCoerceScaleValue((double)value);
            else return value;
        }

        public static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o != null)
                OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        public static double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        public static void OnScaleValueChanged(double oldValue, double newValue) { }

        public static double CalculateScale(Window window, float scaleY, float scaleX)
        {
            double yScale = window.ActualHeight / scaleY;
            double xScale = window.ActualWidth / scaleX;
            double value = Math.Min(xScale, yScale);

            return (double)OnCoerceScaleValue(window, value);
        }
    }
}
