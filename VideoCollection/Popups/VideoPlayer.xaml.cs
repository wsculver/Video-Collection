using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using VideoCollection.Helpers;
using VideoCollection.Movies;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for VideoPlayer.xaml
    /// </summary>
    public partial class VideoPlayer : Window
    {
        private bool _userIsDraggingSlider = false;
        private double _restoreVolume = 0.5;

        // Don't use this constructur. It is only here to make resizing work
        public VideoPlayer() { }

        public VideoPlayer(MovieDeserialized movie)
        {
            InitializeComponent();

            meVideoPlayer.Source = new Uri(movie.MovieFilePath);
            labelTitle.Content = movie.Title;
            txtDuration.Text = movie.Runtime;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();

            sliProgress.ApplyTemplate();
            Thumb thumb = (sliProgress.Template.FindName("PART_Track", sliProgress) as Track).Thumb;
            thumb.MouseEnter += new MouseEventHandler(thumb_MouseEnter);

            meVideoPlayer.Play();
        }

        // Scale based on the size of the window
        private static ScaleValueHelper _scaleValueHelper = new ScaleValueHelper();
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = _scaleValueHelper.SetScaleValueProperty<VideoPlayer>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = _scaleValueHelper.CalculateScale(videoPlayerWindow, 450f, 800f);
        }

        // Tick to show video progress
        private void timer_Tick(object sender, EventArgs e)
        {
            if ((meVideoPlayer.Source != null) && (meVideoPlayer.NaturalDuration.HasTimeSpan) && (!_userIsDraggingSlider))
            {
                sliProgress.Minimum = 0;
                sliProgress.Maximum = meVideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliProgress.Value = meVideoPlayer.Position.TotalSeconds;
            }
        }

        // Stop ticking while dragging the slider
        private void sliProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            _userIsDraggingSlider = true;
        }

        // Set the video position when done sliding
        private void sliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _userIsDraggingSlider = false;
            meVideoPlayer.Position = TimeSpan.FromSeconds(sliProgress.Value);
        }

        // Set the display time
        private void sliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtTime.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(@"h\:mm\:ss");
        }

        // Allow clicking the slider to set a position
        private void thumb_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.MouseDevice.Captured == null)
            {
                MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left);
                args.RoutedEvent = MouseLeftButtonDownEvent;
                (sender as Thumb).RaiseEvent(args);
            }
        }

        // Close the video player
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Play the video
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            meVideoPlayer.Play();
            btnPlay.Visibility = Visibility.Collapsed;
            btnPause.Visibility = Visibility.Visible;
        }

        // Pause the video
        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            meVideoPlayer.Pause();
            btnPause.Visibility = Visibility.Collapsed;
            btnPlay.Visibility = Visibility.Visible;
        }

        // Mute the video
        private void btnMute_Click(object sender, RoutedEventArgs e)
        {
            meVideoPlayer.IsMuted = true;
            _restoreVolume = meVideoPlayer.Volume;
            meVideoPlayer.Volume = 0.0;
            btnMute.Visibility = Visibility.Collapsed;
            btnUnMute.Visibility = Visibility.Visible;
        }

        // Unmute the video
        private void btnUnMute_Click(object sender, RoutedEventArgs e)
        {
            meVideoPlayer.IsMuted = false;
            meVideoPlayer.Volume = _restoreVolume;
            btnUnMute.Visibility = Visibility.Collapsed;
            btnMute.Visibility = Visibility.Visible;
        }
    }
}
