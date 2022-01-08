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
        private bool _expanded = true;
        private bool _playing = true;
        private double _restoreVolume = 0.5;
        public double LeftMultiplier;
        public double TopMultiplier;

        // Don't use this constructur. It is only here to make resizing work
        public VideoPlayer() { }

        public VideoPlayer(MovieDeserialized movie)
        {
            InitializeComponent();

            updateVideo(movie);
        }

        // Allow the video in the player to be updated
        public void updateVideo(MovieDeserialized movie)
        {
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
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<VideoPlayer>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion

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
            if(!_playing)
            {
                meVideoPlayer.Play();
                meVideoPlayer.Pause();
            }
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
            App.videoPlayer = null;
            Close();
        }

        // Play/Pause the video
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if(_playing)
            {
                _playing = false;
                meVideoPlayer.Pause();
                iconPlay.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                iconPlay.Width = 44;
                iconPlay.Height = 44;
                iconPlay.Margin = new Thickness(16, 0, 0, 26);
            }
            else
            {
                _playing = true;
                meVideoPlayer.Play();
                iconPlay.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
                iconPlay.Width = 40;
                iconPlay.Height = 40;
                iconPlay.Margin = new Thickness(22, 0, 0, 26);
            }
        }

        // Mute the video
        private void btnMute_Click(object sender, RoutedEventArgs e)
        {
            if(meVideoPlayer.IsMuted)
            {
                meVideoPlayer.IsMuted = false;
                meVideoPlayer.Volume = _restoreVolume;
                iconMute.Kind = MaterialDesignThemes.Wpf.PackIconKind.VolumeHigh;
            }
            else
            {
                meVideoPlayer.IsMuted = true;
                _restoreVolume = meVideoPlayer.Volume;
                meVideoPlayer.Volume = 0.0;
                iconMute.Kind = MaterialDesignThemes.Wpf.PackIconKind.Mute;
            }
        }

        // Shrink/Expand the video player 
        private void btnExpand_Click(object sender, RoutedEventArgs e)
        {
            if(_expanded)
            {
                _expanded = false;
                // Shift overlay to look good with widescreen videos
                if (WindowState == WindowState.Maximized && Owner.WindowState == WindowState.Normal)
                {
                    ScaleValue = ScaleValueHelper.CalculateScale(videoPlayerWindow, 1800f, 3200f);
                }
                gridOverlay.Margin = new Thickness(ScaleValue * -30, ScaleValue * -20, ScaleValue * -30, ScaleValue * -30);
                iconExpand.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowExpand;
                WindowState = WindowState.Normal;
                iconFullScreen.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowExpandAll;
                Width = Owner.Width * 0.4;
                Height = Width * 0.5625;
                Left = Owner.Left + (Owner.Width - (Owner.Width * 0.015625) - Width);
                LeftMultiplier = (Left - Owner.Left) / Owner.Width;
                Top = Owner.Top + (Owner.Height - (Owner.Width * 0.015625) - Height);
                TopMultiplier = (Top - Owner.Top) / Owner.Height;
                Topmost = true;
            } 
            else
            {
                _expanded = true;
                Topmost = false;
                iconExpand.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowCollapse;
                Width = Owner.Width;
                Height = Owner.Height;
                Left = LeftMultiplier = Owner.Left;
                Top = TopMultiplier = Owner.Top;
                gridOverlay.Margin = new Thickness(0, 0, 0, 0);
            }
        }

        // Maximize/Restore the window
        private void btnFullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                iconFullScreen.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowExpandAll;
            }
            else
            {
                WindowState = WindowState.Maximized;
                iconFullScreen.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowCollapseAll;
                _expanded = true;
                iconExpand.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowCollapse;
                Width = Owner.Width;
                Height = Owner.Height;
                Left = LeftMultiplier = Owner.Left;
                Top = TopMultiplier = Owner.Top;
                gridOverlay.Margin = new Thickness(0, 0, 0, 0);
            }
        }

        private void gridOverlay_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_expanded)
            {
                ScaleValue = ScaleValueHelper.CalculateScale(videoPlayerWindow, 900f, 1600f);
            }
            else
            {
                Height = Width * 0.5625;
            }
        }
    }
}
