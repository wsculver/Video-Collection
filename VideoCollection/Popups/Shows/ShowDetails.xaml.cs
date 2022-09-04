using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using VideoCollection.CustomTypes;
using VideoCollection.Database;
using VideoCollection.Helpers;
using VideoCollection.Shows;

namespace VideoCollection.Popups.Shows
{
    /// <summary>
    /// Interaction logic for ShowDetails.xaml
    /// </summary>
    public partial class ShowDetails : Window, ScaleableWindow
    {
        private static int _scrollViewerMargins = 24;
        private static int _sideMargins = 16;
        private double _scrollDistance = 0;
        private int _showId;
        private int _selectedSeasonIndex = 0;
        private Border _splash;
        private Action _callback;
        private DispatcherTimer _timer;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public ShowDetails() { }

        public ShowDetails(int id, ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _callback = callback;

            WidthScale = 0.93;
            HeightScale = 0.85;
            HeightToWidthRatio = 0.489;

            Show show = DatabaseFunctions.GetShow(id);
            _showId = show.Id;
            try
            {
                ShowDeserialized showDeserialized = new ShowDeserialized(show);
                labelTitle.Content = show.Title;
                imageShowThumbnail.Source = showDeserialized.Thumbnail;
                if (show.Rating == "")
                {
                    labelRating.Visibility = Visibility.Collapsed;
                    txtRating.Visibility = Visibility.Collapsed;
                }
                txtRating.Text = show.Rating;
                string categories = "";
                List<string> categoriesList = JsonConvert.DeserializeObject<List<string>>(show.Categories);
                categoriesList.Sort();
                foreach (string category in categoriesList)
                {
                    categories += (CultureInfo.CurrentCulture.TextInfo.ToTitleCase(category.ToLower()) + ", ");
                }
                categories = categories.Substring(0, categories.Length - 2);
                txtCategories.Text = categories;
                cmbSeasons.DisplayMemberPath = "SeasonName";
                cmbSeasons.SelectedValuePath = "SeasonNumber";
                List<ShowSeason> showSeasons = JsonConvert.DeserializeObject<List<ShowSeason>>(show.Seasons);
                cmbSeasons.ItemsSource = showSeasons;
                cmbSeasons.Text = "Select Season";
                cmbSeasons.SelectedIndex = 0;
                ShowVideo nextEpisode = JsonConvert.DeserializeObject<ShowVideo>(show.NextEpisode);
                DataContext = nextEpisode;
            }
            catch (Exception ex)
            {
                Messages.Error(ex.Message, ref Splash);
                _callback();
            }

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(3000);
            _timer.Tick += timer_Tick;
            _timer.Start();

            UpdateVideoScrollButtons();
            UpdateSectionsScrollButtons();
        }

        // Update the next episode
        private void timer_Tick(object sender, EventArgs e)
        {
            // TODO: Optimize this to only query DB when needed
            Show show = DatabaseFunctions.GetShow(_showId);
            ShowVideo nextEpisode = JsonConvert.DeserializeObject<ShowVideo>(show.NextEpisode);
            DataContext = nextEpisode;
        }

        // Scale based on the size of the window
            #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<ShowDetails>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            double columnWidth = 0;

            if (columnWidth == 0)
            {
                columnWidth = _scrollViewerMargins;
                double tileWidth = 154;
                while (columnWidth + tileWidth + _sideMargins < MainGrid.ActualWidth)
                {
                    columnWidth += tileWidth;
                }
            }

            colMiddle.Width = new GridLength(columnWidth);

            _scrollDistance = columnWidth - _scrollViewerMargins;

            ScaleValue = ScaleValueHelper.CalculateScale(showDetailsWindow, 400f, 780f);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _splash.Visibility = Visibility.Collapsed;
            _timer.Stop();
            _callback();
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            Close();
        }

        private void imageShowThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                Show show = DatabaseFunctions.GetShow(_showId);
                if (App.videoPlayer == null)
                {
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    try
                    {
                        VideoPlayer popup = new VideoPlayer(show);
                        StaticHelpers.ShowVideoPlayer(popup);
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox popup = new CustomMessageBox(ex.Message, CustomMessageBox.MessageBoxType.OK);
                        popup.scaleWindow(parentWindow);
                        parentWindow.addChild(popup);
                        popup.Owner = parentWindow;
                        popup.ShowDialog();
                    }
                }
                else
                {
                    App.videoPlayer.updateVideo(show);
                }
            }
        }

        private void imageThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                string[] split = (sender as Image).Tag.ToString().Split(new[] {",,,"}, StringSplitOptions.None);
                ShowVideo showVideo = new ShowVideo()
                {
                    Title = split[0],
                    FilePath = split[1],
                    Runtime = split[2],
                    Subtitles = split[3],
                    Commentaries = split[4],
                    DeletedScenes = split[5],
                    ShowTitle = split[6],
                    NextEpisode = split[7],
                    IsBonusVideo = !split[8].ToLower().Equals("episodes")
                };

                if (App.videoPlayer == null)
                {
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    try
                    {
                        VideoPlayer popup = new VideoPlayer(showVideo);
                        StaticHelpers.ShowVideoPlayer(popup);
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox popup = new CustomMessageBox(ex.Message, CustomMessageBox.MessageBoxType.OK);
                        popup.scaleWindow(parentWindow);
                        parentWindow.addChild(popup);
                        popup.Owner = parentWindow;
                        popup.ShowDialog();
                    }
                }
                else
                {
                    App.videoPlayer.updateVideo(showVideo);
                }
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            AnimatedScrollViewer scroll = StaticHelpers.GetObject<AnimatedScrollViewer>((sender as Button).Parent);
            double location = scroll.HorizontalOffset;

            if (Math.Round(location - _scrollDistance) <= 0)
            {
                location = 0;
            }
            else
            {
                location -= _scrollDistance;
            }

            var scrolling = new DoubleAnimation(scroll.ContentHorizontalOffset, location, new Duration(TimeSpan.FromMilliseconds(400)));
            scroll.BeginAnimation(AnimatedScrollViewer.SetableOffsetProperty, scrolling);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            AnimatedScrollViewer scroll = StaticHelpers.GetObject<AnimatedScrollViewer>((sender as Button).Parent);
            double location = scroll.HorizontalOffset;

            if (Math.Round(location + _scrollDistance) >= Math.Round(scroll.ScrollableWidth))
            {
                location = scroll.ScrollableWidth;
            }
            else
            {
                location += _scrollDistance;
            }

            var scrolling = new DoubleAnimation(scroll.ContentHorizontalOffset, location, new Duration(TimeSpan.FromMilliseconds(400)));
            scroll.BeginAnimation(AnimatedScrollViewer.SetableOffsetProperty, scrolling);
        }

        // Make the video scroll buttons visable if they are needed
        private void UpdateVideoScrollButtons()
        {
            ContentPresenter c2 = (ContentPresenter)icVideos.ItemContainerGenerator.ContainerFromIndex(0);
            if (c2 != null)
            {
                c2.ApplyTemplate();
                Image tile = c2.ContentTemplate.FindName("imageThumbnail", c2) as Image;
                if (Math.Round(icVideos.Items.Count * (tile.ActualWidth + tile.Margin.Left + tile.Margin.Right)) > Math.Round(scrollVideos.ActualWidth) && scrollVideos.HorizontalOffset < scrollVideos.ScrollableWidth)
                {
                    btnNext.Visibility = Visibility.Visible;
                }
                else
                {
                    btnNext.Visibility = Visibility.Hidden;
                }

                if (scrollVideos.HorizontalOffset > 0)
                {
                    btnPrevious.Visibility = Visibility.Visible;
                }
                else
                {
                    btnPrevious.Visibility = Visibility.Hidden;
                }
            }
        }

        private void scrollVideos_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateVideoScrollButtons();
        }

        private void btnSection_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            for (int i = 0; i < icSectionButtons.Items.Count; i++)
            {
                ContentPresenter c = (ContentPresenter)icSectionButtons.ItemContainerGenerator.ContainerFromIndex(i);
                if (c != null)
                {
                    c.ApplyTemplate();
                    Button button = c.ContentTemplate.FindName("btnSection", c) as Button;
                    if(button.Equals(clickedButton))
                    {
                        button.Background = Application.Current.Resources["SelectedButtonBackgroundBrush"] as SolidColorBrush;
                    }
                    else
                    {
                        button.Background = null;
                    }
                }
            }

            scrollVideos.ScrollToHorizontalOffset(0);
            icVideos.ItemsSource = getSectionVideos(clickedButton.Content.ToString());

            UpdateVideoScrollButtons();
        }

        private void imageShowThumbnail_MouseEnter(object sender, MouseEventArgs e)
        {
            playShowIconBackground.Visibility = Visibility.Visible;
            iconPlayShow.Visibility = Visibility.Visible;
            showSplash.Visibility = Visibility.Visible;
        }

        private void imageShowThumbnail_MouseLeave(object sender, MouseEventArgs e)
        {
            playShowIconBackground.Visibility = Visibility.Collapsed;
            iconPlayShow.Visibility = Visibility.Collapsed;
            showSplash.Visibility = Visibility.Collapsed;
        }

        private void imageThumbnail_MouseEnter(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>((sender as Image).Parent, "rectPlayBackground").Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "iconPlayVideo").Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "videoSplash").Visibility = Visibility.Visible;
        }

        private void imageThumbnail_MouseLeave(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>((sender as Image).Parent, "rectPlayBackground").Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "iconPlayVideo").Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "videoSplash").Visibility = Visibility.Collapsed;
        }

        public void scaleWindow(Window parent)
        {
            Width = parent.ActualWidth * WidthScale;
            Height = Width * HeightToWidthRatio;
            if (Height > parent.ActualHeight * HeightScale)
            {
                Height = parent.ActualHeight * HeightScale;
                Width = Height / HeightToWidthRatio;
            }

            Left = parent.Left + (parent.Width - ActualWidth) / 2;
            Top = parent.Top + (parent.Height - ActualHeight) / 2;
        }

        private void cmbSeasons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scrollVideos.ScrollToLeftEnd();
            _selectedSeasonIndex = (sender as ComboBox).SelectedItem == null ? 1 : (sender as ComboBox).SelectedIndex;
            Show show = DatabaseFunctions.GetShow(_showId);
            ShowSeasonDeserialized showSeason = show.getSeasons().ElementAt(_selectedSeasonIndex);
            icSectionButtons.ItemsSource = showSeason.Sections;
            if (showSeason.Sections.Any())
            {
                showSeason.Sections.FirstOrDefault().Background = Application.Current.Resources["SelectedButtonBackgroundBrush"] as SolidColorBrush;
                icVideos.ItemsSource = getSectionVideos();
            }
        }

        private List<ShowVideoDeserialized> getSectionVideos(string section = null)
        {
            List<ShowVideoDeserialized> sectionVideos = new List<ShowVideoDeserialized>();
            Show show = DatabaseFunctions.GetShow(_showId);
            List<ShowSeasonDeserialized> showSeasons = show.getSeasons();
            ShowSeasonDeserialized season = showSeasons.ElementAt(_selectedSeasonIndex);
            if (section == null)
            {
                section = season.Sections.FirstOrDefault().Name;
            }
            foreach (ShowVideoDeserialized video in season.Videos)
            {
                if (video.Section == section)
                {
                    sectionVideos.Add(video);
                }
            }

            return sectionVideos;
        }

        // Make the sections scroll buttons visable if they are needed
        private void UpdateSectionsScrollButtons()
        {
            double totalWidth = 0.0;
            int numSections = icSectionButtons.Items.Count;
            for (int i = 0; i < numSections; i++)
            {
                ContentPresenter c = (ContentPresenter)icSectionButtons.ItemContainerGenerator.ContainerFromIndex(i);
                if (c != null)
                {
                    c.ApplyTemplate();
                    Button button = c.ContentTemplate.FindName("btnSection", c) as Button;
                    totalWidth += button.ActualWidth + button.Margin.Left + button.Margin.Right;
                }
            }

            if (totalWidth > Math.Round(scrollSections.ActualWidth) && scrollSections.HorizontalOffset < scrollSections.ScrollableWidth)
            {
                btnSectionsNext.Visibility = Visibility.Visible;
            }
            else
            {
                btnSectionsNext.Visibility = Visibility.Hidden;
            }

            if (scrollSections.HorizontalOffset > 0)
            {
                btnSectionsPrevious.Visibility = Visibility.Visible;
            }
            else
            {
                btnSectionsPrevious.Visibility = Visibility.Hidden;
            }
        }

        private void scrollSections_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateSectionsScrollButtons();
        }
    }
}
