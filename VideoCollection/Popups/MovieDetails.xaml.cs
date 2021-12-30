using SQLite;
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
using System.Windows.Shapes;
using VideoCollection.Helpers;
using VideoCollection.Movies;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for MovieDetails.xaml
    /// </summary>
    public partial class MovieDetails : Window
    {
        private MovieDeserialized _movieDeserialized;
        private Dictionary<string, List<MovieBonusVideoDeserialized>> _bonusVideosDictionary;

        // Don't use this constructur. It is only here to make resizing work
        public MovieDetails() { }

        public MovieDetails(string Id)
        {
            InitializeComponent();

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Movie>();
                Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + Id)[0];
                _movieDeserialized = new MovieDeserialized(movie);
                labelTitle.Content = _movieDeserialized.Title.ToUpper();
                imageMovieThumbnail.Source = _movieDeserialized.Thumbnail;
                _movieDeserialized.BonusSections.FirstOrDefault().Background = new SolidColorBrush(Color.FromRgb(75, 75, 75));
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
                icBonusVideos.ItemsSource = _bonusVideosDictionary[_movieDeserialized.BonusSections.FirstOrDefault().Name];
            }

            UpdateBonusScrollButtons();
        }

        // Scale based on the size of the window
        private static ScaleValueHelper _scaleValueHelper = new ScaleValueHelper();
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = _scaleValueHelper.SetScaleValueProperty<MovieDetails>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = _scaleValueHelper.CalculateScale(movieDetailsWindow, 400f, 700f);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void imageMovieThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void imageThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button);
            Image tile = StaticHelpers.GetObject<Image>(button.Parent) as Image;
            double tileWidth = 0;
            if (tile != null)
            {
                tileWidth = tile.ActualWidth + tile.Margin.Left + tile.Margin.Right;
            }
            ScrollViewer scroll = StaticHelpers.GetObject<ScrollViewer>(button.Parent) as ScrollViewer;
            double location = scroll.HorizontalOffset;

            if (Math.Round(location - tileWidth) <= 0)
            {
                location = 0;
            }
            else
            {
                location -= tileWidth;
            }

            scroll.ScrollToHorizontalOffset(location);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button);
            Image tile = StaticHelpers.GetObject<Image>(button.Parent) as Image;
            double tileWidth = 0;
            if (tile != null)
            {
                tileWidth = tile.ActualWidth + tile.Margin.Left + tile.Margin.Right;
            }
            ScrollViewer scroll = StaticHelpers.GetObject<ScrollViewer>(button.Parent) as ScrollViewer;
            double location = scroll.HorizontalOffset;

            if (Math.Round(location + tileWidth) >= Math.Round(scroll.ScrollableWidth))
            {
                location = scroll.ScrollableWidth;
            }
            else
            {
                location += tileWidth;
            }

            scroll.ScrollToHorizontalOffset(location);
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
    }
}
