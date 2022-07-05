using System.Windows;
using System.Windows.Controls;

namespace VideoCollection.CustomTypes
{
    public class AnimatedScrollViewer : ScrollViewer
    {
        public static readonly DependencyProperty SetableOffsetProperty =
        DependencyProperty.Register("SetableOffset", typeof(double), typeof(AnimatedScrollViewer),
        new PropertyMetadata(new PropertyChangedCallback(OnChanged)));

        public double SetableOffset
        {
            get { return (double)this.GetValue(ScrollViewer.ContentHorizontalOffsetProperty); }
            set { this.ScrollToHorizontalOffset(value); }
        }

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AnimatedScrollViewer)d).SetableOffset = (double)e.NewValue;
        }
    }
}
