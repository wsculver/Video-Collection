using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using VideoCollection.Helpers;
using VideoCollection.Movies;
using VideoCollection.Subtitles;

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
        private bool _videoPlayerMoved = false;
        private double _restoreVolume = 0.5;
        private double _restoreLeftMultiplier = 0;
        private double _restoreTopMultiplier = 0;
        private DispatcherTimer _overlayHideTimer;
        private int _hideOverlaySeconds = 5;
        private List<SubtitleSegment> _subtitles;
        private int _subtitleIndex = 0;
        private bool _subtitlesOn = false;
        private string _timeFormat = "";
        private HwndSource _mSource;
        private DispatcherTimer _imageFrameTimer;
        private int _imageFrameMilliseconds = 5;
        private Point _lastMousePos;

        public double VideoPlayerMargin = 25;
        public double LeftMultiplier = 0;
        public double TopMultiplier = 0;

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
        enum WindowsMessage
        {
            WM_MOVING = 0x0216
        }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public VideoPlayer() { }

        public VideoPlayer(MovieDeserialized movie)
        {
            InitializeComponent();

            updateVideo(movie);
        }

        public VideoPlayer(MovieBonusVideoDeserialized movieBonusVideo)
        {
            InitializeComponent();

            updateVideo(movieBonusVideo);
        }

        // Allow the video in the player to be updated
        private void update()
        {
            if (_subtitles.Count == 0)
            {
                _subtitlesOn = false;
                rectSubtitlesEnabled.Visibility = Visibility.Hidden;
                btnSubtitles.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnSubtitles.Visibility = Visibility.Visible;
            }

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += timer_Tick;
            timer.Start();

            _overlayHideTimer = new DispatcherTimer();
            _overlayHideTimer.Interval = TimeSpan.FromSeconds(1);
            _overlayHideTimer.Tick += hideOverlayTimer_Tick;

            _imageFrameTimer = new DispatcherTimer();
            _imageFrameTimer.Interval = TimeSpan.FromMilliseconds(1);
            _imageFrameTimer.Tick += imageFrameTimer_Tick;

            sliProgress.ApplyTemplate();
            Thumb thumb = sliProgress.Template.FindName("thumb", sliProgress) as Thumb;
            thumb.MouseEnter += thumb_MouseEnter;

            _lastMousePos = new Point(0, 0);

            if (!File.Exists(meVideoPlayer.Source.ToString().Substring(8)))
            {
                throw new Exception("Video file not found. Make sure it is in the same location it was when you added it.");
            }

            meVideoPlayer.Play();
        }
        public void updateVideo(MovieDeserialized movie)
        {
            meVideoPlayer.Source = new Uri(movie.MovieFilePath);
            txtTitle.Text = movie.Title;
            txtDuration.Text = movie.Runtime;
            setTimeFormat(movie.Runtime);
            _subtitles = movie.Subtitles;
            update();
        }
        public void updateVideo(MovieBonusVideoDeserialized movieBonusVideo)
        {
            meVideoPlayer.Source = new Uri(movieBonusVideo.FilePath);
            txtTitle.Text = movieBonusVideo.Title;
            txtDuration.Text = movieBonusVideo.Runtime;
            setTimeFormat(movieBonusVideo.Runtime);
            _subtitles = movieBonusVideo.Subtitles;
            update();
        }

        // Set the left side of the time display based on the runtime of the video
        public void setTimeFormat(string runtime)
        {
            switch (runtime.Length)
            {
                case 7:
                    _timeFormat = @"h\:mm\:ss";
                    break;
                case 5:
                    _timeFormat = @"mm\:ss";
                    break;
                case 4:
                    _timeFormat = @"m\:ss";
                    break;
                default:
                    _timeFormat = @"hh\:mm\:ss";
                    break;
            }
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<VideoPlayer>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }

        public static readonly DependencyProperty SubtitlesScaleValueProperty = DependencyProperty.Register("SubtitlesScaleValue", typeof(double), typeof(VideoPlayer), new UIPropertyMetadata(1.0, new PropertyChangedCallback(ScaleValueHelper.OnScaleValueChanged), new CoerceValueCallback(ScaleValueHelper.OnCoerceScaleValue)));
        public double SubtitlesScaleValue
        {
            get => (double)GetValue(SubtitlesScaleValueProperty);
            set => SetValue(SubtitlesScaleValueProperty, value);
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

                txtSubtitles.Inlines.Clear();
                if (_subtitlesOn)
                {
                    if (TimeSpan.TryParseExact(_subtitles[_subtitleIndex].Start, @"hh\:mm\:ss\:fff", DateTimeFormatInfo.InvariantInfo, out var start) &&
                        TimeSpan.TryParseExact(_subtitles[_subtitleIndex].End, @"hh\:mm\:ss\:fff", DateTimeFormatInfo.InvariantInfo, out var end) &&
                        start < end)
                    {
                        if (meVideoPlayer.Position >= start && meVideoPlayer.Position < end)
                        {
                            string text = _subtitles[_subtitleIndex].Content;
                            string[] lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None);

                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].Contains("<i>"))
                                {
                                    string line = lines[i];
                                    while (line.Contains("<i>"))
                                    {
                                        int openTagIndex = line.IndexOf("<i>");
                                        int closeTagIndex = line.IndexOf("</i>");
                                        txtSubtitles.Inlines.Add(line.Substring(0, openTagIndex));
                                        txtSubtitles.Inlines.Add(new Run(line.Substring(openTagIndex + 3, closeTagIndex - (openTagIndex + 3))) { FontStyle = FontStyles.Italic });
                                        line = line.Substring(closeTagIndex + 4);
                                    }
                                    txtSubtitles.Inlines.Add(line);
                                }
                                else
                                {
                                    txtSubtitles.Inlines.Add(lines[i]);
                                }

                                if (i < lines.Length - 2)
                                {
                                    txtSubtitles.Inlines.Add("\r\n");
                                }
                            }
                        }
                        else if (meVideoPlayer.Position >= end)
                        {
                            if (_subtitleIndex + 1 < _subtitles.Count)
                            {
                                _subtitleIndex++;
                            }
                        }
                    }
                }
            }
        }

        // Tick to countdown _hideOverlaySeconds
        private void hideOverlayTimer_Tick(object sender, EventArgs e)
        {
            _hideOverlaySeconds--;
            if (_hideOverlaySeconds == 0)
            {
                _overlayHideTimer.Stop();
                // Only hide the overlay if the video is playing and expanded
                if (_playing && _expanded)
                {
                    gridOverlay.Visibility = Visibility.Collapsed;
                    borderSubtitles.Margin = new Thickness(0, 0, 0, 30);
                    Cursor = Cursors.None;
                }
            }
        }

        // Tick to countdown _imageFrameMilliseconds
        private void imageFrameTimer_Tick(object sender, EventArgs e)
        {
            _imageFrameMilliseconds--;
            if (_imageFrameMilliseconds == 0)
            {
                _imageFrameTimer.Stop();

                Track track = sliProgress.Template.FindName("PART_Track", sliProgress) as Track;
                TimeSpan time = TimeSpan.FromSeconds(track.ValueFromPoint(_lastMousePos));
                using (MemoryStream thumbnailStream = new MemoryStream())
                {
                    StaticHelpers.CreateThumbnailFromVideoFile(thumbnailStream, meVideoPlayer.Source.OriginalString, (int)time.TotalSeconds);
                    System.Drawing.Image image = System.Drawing.Image.FromStream(thumbnailStream);
                    imgVideoFrame.Source = StaticHelpers.ImageToImageSource(image);
                }
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
            for (int i = 0; i < _subtitles.Count; i++)
            {
                if (TimeSpan.TryParseExact(_subtitles[i].Start, @"hh\:mm\:ss\:fff", DateTimeFormatInfo.InvariantInfo, out var start) &&
                    TimeSpan.TryParseExact(_subtitles[i].End, @"hh\:mm\:ss\:fff", DateTimeFormatInfo.InvariantInfo, out var end) &&
                    start < end)
                {
                    if (meVideoPlayer.Position <= end)
                    {
                        _subtitleIndex = i;
                        break;
                    }
                }
            }
        }

        // Set the display time
        private void sliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtTime.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(_timeFormat);
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
                gridOverlay.Margin = new Thickness(ScaleValue * -30, ScaleValue * -15, ScaleValue * -30, ScaleValue * -25);
                iconExpand.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowExpand;
                WindowState = WindowState.Normal;
                iconFullScreen.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowExpandAll;
                Width = Owner.ActualWidth * 0.4;
                Height = Width * 0.5625;
                VideoPlayerMargin = Owner.ActualWidth * 0.015625;
                if (!_videoPlayerMoved)
                {
                    Left = Owner.Left + (Owner.ActualWidth - VideoPlayerMargin - Width);
                    LeftMultiplier = (Left - Owner.Left) / Owner.ActualWidth;
                    Top = Owner.Top + (Owner.ActualHeight - VideoPlayerMargin - Height);
                    TopMultiplier = (Top - Owner.Top) / Owner.ActualHeight;
                }
                else
                {
                    Left = Owner.Left + (Owner.ActualWidth * _restoreLeftMultiplier);
                    Top = Owner.Top + (Owner.ActualHeight * _restoreTopMultiplier);
                }
                borderSubtitles.Margin = new Thickness(0, 0, 0, 280);
                Topmost = true;
            } 
            else
            {
                _expanded = true;
                Topmost = false;
                iconExpand.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowCollapse;
                Width = Owner.ActualWidth;
                Height = Owner.ActualHeight;
                Left = Owner.Left;
                Top = Owner.Top;
                LeftMultiplier = Owner.Left;
                TopMultiplier = Owner.Top;
                gridOverlay.Margin = new Thickness(0, 0, 0, 0);
                borderSubtitles.Margin = new Thickness(0, 0, 0, 150);
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
                Topmost = false;
                iconExpand.Kind = MaterialDesignThemes.Wpf.PackIconKind.ArrowCollapse;
                Width = Owner.ActualWidth;
                Height = Owner.ActualHeight;
                Left = Owner.Left;
                Top = Owner.Top;
                LeftMultiplier = Owner.Left;
                TopMultiplier = Owner.Top;
                gridOverlay.Margin = new Thickness(0, 0, 0, 0);
                borderSubtitles.Margin = new Thickness(0, 0, 0, 150);
            }
        }

        // Scale the overlay correctly
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

        // Allow the video player to be dragged
        private void videoPlayerWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_expanded && e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _mSource = (HwndSource)PresentationSource.FromVisual(this);
            _mSource.AddHook(WndProc);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_mSource != null)
            {
                _mSource.RemoveHook(WndProc);
                _mSource.Dispose();
                _mSource = null;
            }

            base.OnClosed(e);
        }

        // Prevent the video player from moving outside the main window when draggin it
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)WindowsMessage.WM_MOVING)
            {
                // The window dimensions are off in this function for some reason so this is a correction factor
                double leftMultiplier = 1.255;
                double rightMultiplier = 1.252;
                double topMultiplier = 1.255;
                double bottomMultiplier = 1.253;
                RECT bounds = new RECT() { Left = (int)((Owner.Left + VideoPlayerMargin) * leftMultiplier), Top = (int)((Owner.Top + VideoPlayerMargin) * topMultiplier), Right = (int)((Owner.Left + Owner.ActualWidth - VideoPlayerMargin) * rightMultiplier), Bottom = (int)((Owner.Top + Owner.ActualHeight - VideoPlayerMargin) * bottomMultiplier) };

                RECT window = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
                if (window.Left < bounds.Left)
                {
                    window.Right = window.Right + bounds.Left - window.Left;
                    window.Left = bounds.Left;
                }
                if (window.Top < bounds.Top)
                {
                    window.Bottom = window.Bottom + bounds.Top - window.Top;
                    window.Top = bounds.Top;
                }
                if (window.Right >= bounds.Right)
                {
                    window.Left = bounds.Right - window.Right + window.Left - 1;
                    window.Right = bounds.Right - 1;
                }
                if (window.Bottom >= bounds.Bottom)
                {
                    window.Top = bounds.Bottom - window.Bottom + window.Top - 1;
                    window.Bottom = bounds.Bottom - 1;
                }
                Marshal.StructureToPtr(window, lParam, true);

                handled = true;
                return new IntPtr(1);
            }

            handled = false;
            return IntPtr.Zero;
        }

        // Keep track of where the video player is when moving the main window
        private void videoPlayerWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_expanded)
            {
                _videoPlayerMoved = true;
                LeftMultiplier = (Left - Owner.Left) / Owner.ActualWidth;
                TopMultiplier = (Top - Owner.Top) / Owner.ActualHeight;
                _restoreLeftMultiplier = LeftMultiplier;
                _restoreTopMultiplier = TopMultiplier;
            }
        }

        // Make sure the video player doesn't go outside of the main window when resizing the main window
        private void videoPlayerWindow_LocationChanged(object sender, EventArgs e)
        {
            if(!_expanded && Width < Owner.ActualWidth - (2 * VideoPlayerMargin) && Height < Owner.ActualHeight - (2 * VideoPlayerMargin))
            {
                bool changeMultipliers = false;
                if (Left + Width > Owner.Left + Owner.ActualWidth - VideoPlayerMargin)
                {
                    Left = Owner.Left + Owner.ActualWidth - VideoPlayerMargin - Width;
                    changeMultipliers = true;
                }

                if (Left < Owner.Left + VideoPlayerMargin)
                {
                    Left = Owner.Left + VideoPlayerMargin;
                    changeMultipliers = true;
                }

                if (Top + Height > Owner.Top + Owner.ActualHeight - VideoPlayerMargin)
                {
                    Top = Owner.Top + Owner.ActualHeight - VideoPlayerMargin - Height;
                    changeMultipliers = true;
                }

                if (Top < Owner.Top + VideoPlayerMargin)
                {
                    Top = Owner.Top + VideoPlayerMargin;
                    changeMultipliers = true;
                }

                if (changeMultipliers)
                {
                    LeftMultiplier = (Left - Owner.Left) / Owner.ActualWidth;
                    TopMultiplier = (Top - Owner.Top) / Owner.ActualHeight;
                }
            }
        }

        // Moving the mouse shows the overlay and starts a timer to hide it
        private void meVideoPlayer_MouseMove(object sender, MouseEventArgs e)
        {
            gridOverlay.Visibility = Visibility.Visible;
            if(!_expanded)
            {
                borderSubtitles.Margin = new Thickness(0, 0, 0, 280);
            }
            else
            {
                borderSubtitles.Margin = new Thickness(0, 0, 0, 150);
            }
            Cursor = Cursors.Arrow;
            _hideOverlaySeconds = 5;
            _overlayHideTimer.Start();
        }

        // If the video player is collapsed and the mouse leaves it hide the overlay
        private void videoPlayerWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            _overlayHideTimer.Stop();
            if (_playing)
            {
                gridOverlay.Visibility = Visibility.Collapsed;
                borderSubtitles.Margin = new Thickness(0, 0, 0, 30);
            }
        }

        private void gridSubtitles_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SubtitlesScaleValue = ScaleValueHelper.CalculateScale(videoPlayerWindow, 900f, 1600f);
        }

        // Toggle subtitles on/off
        private void btnSubtitles_Click(object sender, RoutedEventArgs e)
        {
            if(_subtitlesOn)
            {
                _subtitlesOn = false;
                rectSubtitlesEnabled.Visibility = Visibility.Hidden;
            }
            else
            {
                _subtitlesOn = true;
                rectSubtitlesEnabled.Visibility = Visibility.Visible;
            }
        }

        // Show a popup with the frame and time at the hovered location
        private void sliProgress_MouseMove(object sender, MouseEventArgs e)
        {
            if (!floatingFrame.IsOpen) { floatingFrame.IsOpen = true; }

            Point currentPos = e.GetPosition(sliProgress);

            if (_lastMousePos != currentPos)
            {
                imgVideoFrame.Source = null;
                _imageFrameMilliseconds = 5;
                _imageFrameTimer.Start();
                _lastMousePos = currentPos;

                Track track = sliProgress.Template.FindName("PART_Track", sliProgress) as Track;

                TimeSpan time = TimeSpan.FromSeconds(track.ValueFromPoint(currentPos));
                hoverTime.Text = time.ToString(_timeFormat);

                floatingFrame.HorizontalOffset = currentPos.X - (floatingFrameBorder.ActualWidth / 2);
                floatingFrame.VerticalOffset = -155;
            }
        }

        // Close the frame preview when the mouse is not over the progress slider
        private void sliProgress_MouseLeave(object sender, MouseEventArgs e)
        {
            floatingFrame.IsOpen = false;
            _imageFrameTimer.Stop();
        }

        // Prevent the overlay from hiding when the mouse is over a control
        private void meVideoPlayer_MouseLeave(object sender, MouseEventArgs e)
        {
            _overlayHideTimer.Stop();
        }
    }
}
