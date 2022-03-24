using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VideoCollection.Helpers;
using VideoCollection.Popups;
using VideoCollection.Views;
using VideoCollection.CustomTypes;

namespace VideoCollection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double _restoreLeft = 0;
        private double _restoreTop = 0;
        private List<ScaleableWindow> _children = new List<ScaleableWindow>();

        public MainWindow()
        {
            InitializeComponent();

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            Height = SystemParameters.MaximizedPrimaryScreenHeight / 2;
            Width = SystemParameters.MaximizedPrimaryScreenWidth / 2;
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<MainWindow>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion

        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(myMainWindow, (float)SystemParameters.MaximizedPrimaryScreenHeight / 2, (float)SystemParameters.MaximizedPrimaryScreenWidth / 2);

            if(App.videoPlayer != null)
            {
                App.videoPlayer.ScaleValue = ScaleValueHelper.CalculateScale(App.videoPlayer.videoPlayerWindow, 344f, 640f);
                App.videoPlayer.videoPlayerWindow.gridOverlay.Margin = new Thickness(App.videoPlayer.ScaleValue * -30, App.videoPlayer.ScaleValue * -15, App.videoPlayer.ScaleValue * -30, App.videoPlayer.ScaleValue * -20);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            if (App.videoPlayer != null) {
                App.videoPlayer.Topmost = false;
            }
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                Left = _restoreLeft;
                Top = _restoreTop;
                WindowState = WindowState.Normal;
                iconMaximize.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowMaximize;
            }
            else
            {
                _restoreLeft = Left;
                _restoreTop = Top;
                Left = 0;
                Top = 0;
                WindowState = WindowState.Maximized;
                iconMaximize.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowRestore;
            }
        }

        private void btnShows_Click(object sender, RoutedEventArgs e)
        {
            btnShows.Background = Application.Current.Resources["SelectedButtonBackgroundBrush"] as SolidColorBrush;
            btnMovies.Background = null;
            contentControl.Content = new ShowsView();
        }

        private void btnMovies_Click(object sender, RoutedEventArgs e)
        {

            btnMovies.Background = Application.Current.Resources["SelectedButtonBackgroundBrush"] as SolidColorBrush;
            btnShows.Background = null;
            contentControl.Content = new MoviesView();
        }

        // Allow the window to be dragged
        private void myMainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void myMainWindow_LocationChanged(object sender, EventArgs e)
        {
            // Scale child windows
            foreach (ScaleableWindow window in _children)
            {
                window.scaleWindow(this);
            }
        }

        private void myMainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // If the window is maximized by dragging the window to the top change the maximize button
            if (WindowState == WindowState.Maximized)
            {
                iconMaximize.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowRestore;
            }

            // Scale child windows
            foreach(ScaleableWindow window in _children)
            {
                window.scaleWindow(this);
            }
        }

        public void addChild(ScaleableWindow child)
        {
            _children.Add(child);
        }

        public void removeChild(ScaleableWindow child)
        {
            _children.Remove(child);
        }

    }
}
