using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VideoCollection.Helpers
{
    internal class ScaleValueHelper
    {
        public DependencyProperty SetScaleValueProperty<T>() where T : DependencyObject, new()
        {
            return DependencyProperty.Register("ScaleValue", typeof(double), typeof(T), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));
        }

        public object OnCoerceScaleValue(DependencyObject o, object value)
        {
            if (o != null)
                return OnCoerceScaleValue((double)value);
            else return value;
        }

        public void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o != null)
                OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue) { }

        public double CalculateScale(Window window, float scaleY, float scaleX)
        {
            double yScale = window.ActualHeight / scaleY;
            double xScale = window.ActualWidth / scaleX;
            double value = Math.Min(xScale, yScale);

            return (double)OnCoerceScaleValue(window, value);
        }
    }
}
