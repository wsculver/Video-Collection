using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VideoCollection.CustomTypes;
using VideoCollection.Helpers;
using VideoCollection.Movies;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for MovieDetails.xaml
    /// </summary>
    public partial class MovieDetails : Window
    {
        private static int _scrollViewerMargins = 24;
        private static int _sideMargins = 16;
        private double _scrollDistance = 0;
        private MovieDeserialized _movieDeserialized;
        private Dictionary<string, List<MovieBonusVideoDeserialized>> _bonusVideosDictionary;
        private Border _splash;
        private Action _callback;

        // Don't use this constructur. It is only here to make resizing work
        public MovieDetails() { }

        public MovieDetails(string Id, ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _callback = callback;

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Movie>();
                Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + Id)[0];
                _movieDeserialized = new MovieDeserialized(movie);
                labelTitle.Content = _movieDeserialized.Title;
                imageMovieThumbnail.Source = _movieDeserialized.Thumbnail;
                txtRuntime.Text = _movieDeserialized.Runtime;
                txtRating.Text = _movieDeserialized.Rating;
                string categories = "";
                _movieDeserialized.Categories.Sort();
                foreach(string category in _movieDeserialized.Categories)
                {
                    categories += (CultureInfo.CurrentCulture.TextInfo.ToTitleCase(category.ToLower()) + ", ");
                }
                categories = categories.Substring(0, categories.Length-2);
                txtCategories.Text = categories;
                icBonusSectionButtons.ItemsSource = _movieDeserialized.BonusSections;
                _bonusVideosDictionary = new Dictionary<string, List<MovieBonusVideoDeserialized>>();
                foreach(MovieBonusVideoDeserialized bonusVideo in _movieDeserialized.BonusVideos)
                {
                    if(!_bonusVideosDictionary.ContainsKey(bonusVideo.Section))
                    {
                        _bonusVideosDictionary.Add(bonusVideo.Section, new List<MovieBonusVideoDeserialized>());
                    } 
                    _bonusVideosDictionary[bonusVideo.Section].Add(bonusVideo);
                }
                if (_movieDeserialized.BonusSections.Any())
                {
                    _movieDeserialized.BonusSections.FirstOrDefault().Background = Application.Current.Resources["SelectedButtonBackgroundBrush"] as SolidColorBrush;
                    icBonusVideos.ItemsSource = _bonusVideosDictionary[_movieDeserialized.BonusSections.FirstOrDefault().Name];
                    separatorBonusTop.Visibility = Visibility.Visible;
                    separatorBonusBottom.Visibility = Visibility.Visible;
                }
            }

            UpdateBonusScrollButtons();
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<MovieDetails>();
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

            ScaleValue = ScaleValueHelper.CalculateScale(movieDetailsWindow, 400f, 780f);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _splash.Visibility = Visibility.Collapsed;
            _callback();
            Close();
        }

        private void imageMovieThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                if (App.videoPlayer == null)
                {
                    Window parentWindow = Application.Current.MainWindow;
                    VideoPlayer popup = new VideoPlayer(_movieDeserialized);
                    App.videoPlayer = popup;
                    popup.Width = parentWindow.Width;
                    popup.Height = parentWindow.Height;
                    popup.Owner = parentWindow;
                    popup.Left = popup.LeftMultiplier = parentWindow.Left;
                    popup.Top = popup.TopMultiplier = parentWindow.Top;
                    popup.Show();
                }
                else
                {
                    App.videoPlayer.updateVideo(_movieDeserialized);
                }
            }
        }

        private void imageThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {

            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button);
            AnimatedScrollViewer scroll = StaticHelpers.GetObject<AnimatedScrollViewer>(button.Parent);
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
            Button button = (sender as Button);
            AnimatedScrollViewer scroll = StaticHelpers.GetObject<AnimatedScrollViewer>(button.Parent);
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

        // Make the bonus scroll buttons visable if they are needed
        private void UpdateBonusScrollButtons()
        {
            ContentPresenter c2 = (ContentPresenter)icBonusVideos.ItemContainerGenerator.ContainerFromIndex(0);
            if (c2 != null)
            {
                c2.ApplyTemplate();
                Image tile = c2.ContentTemplate.FindName("imageThumbnail", c2) as Image;
                if (Math.Round(icBonusVideos.Items.Count * (tile.ActualWidth + tile.Margin.Left + tile.Margin.Right)) > Math.Round(scrollBonusVideos.ActualWidth) && scrollBonusVideos.HorizontalOffset < scrollBonusVideos.ScrollableWidth)
                {
                    btnNext.Visibility = Visibility.Visible;
                }
                else
                {
                    btnNext.Visibility = Visibility.Hidden;
                }

                if (scrollBonusVideos.HorizontalOffset > 0)
                {
                    btnPrevious.Visibility = Visibility.Visible;
                }
                else
                {
                    btnPrevious.Visibility = Visibility.Hidden;
                }
            }
        }

        private void scrollBonusVideos_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateBonusScrollButtons();
        }

        private void btnBonusSection_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            for (int i = 0; i < icBonusSectionButtons.Items.Count; i++)
            {
                ContentPresenter c = (ContentPresenter)icBonusSectionButtons.ItemContainerGenerator.ContainerFromIndex(i);
                if (c != null)
                {
                    c.ApplyTemplate();
                    Button button = c.ContentTemplate.FindName("btnBonusSection", c) as Button;
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

            scrollBonusVideos.ScrollToHorizontalOffset(0);
            icBonusVideos.ItemsSource = _bonusVideosDictionary[clickedButton.Content.ToString()];

            UpdateBonusScrollButtons();
        }

        private void imageMovieThumbnail_MouseEnter(object sender, MouseEventArgs e)
        {
            playMovieIconBackground.Visibility = Visibility.Visible;
            iconPlayMovie.Visibility = Visibility.Visible;
            movieSplash.Visibility = Visibility.Visible;
        }

        private void imageMovieThumbnail_MouseLeave(object sender, MouseEventArgs e)
        {
            playMovieIconBackground.Visibility = Visibility.Collapsed;
            iconPlayMovie.Visibility = Visibility.Collapsed;
            movieSplash.Visibility = Visibility.Collapsed;
        }

        private void imageThumbnail_MouseEnter(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>((sender as Image).Parent).Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "iconPlayBonus").Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "bonusSplash").Visibility = Visibility.Visible;
        }

        private void imageThumbnail_MouseLeave(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>((sender as Image).Parent).Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "iconPlayBonus").Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "bonusSplash").Visibility = Visibility.Collapsed;
        }
    }
}
