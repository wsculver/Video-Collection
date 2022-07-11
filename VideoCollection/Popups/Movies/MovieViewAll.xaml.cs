using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using VideoCollection.Database;
using VideoCollection.Helpers;
using VideoCollection.Movies;
using VideoCollection.CustomTypes;

namespace VideoCollection.Popups.Movies
{
    /// <summary>
    /// Interaction logic for ViewAll.xaml
    /// </summary>
    public partial class MovieViewAll : Window, ScaleableWindow
    {
        private int _categoryId;
        private bool _categoryChanged = false;
        private Border _splash;
        private Action _callback;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public MovieViewAll() { }

        public MovieViewAll(int id, ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _categoryId = id;
            _splash = splash;
            _callback = callback;

            WidthScale = 0.93;
            HeightScale = 0.85;
            HeightToWidthRatio = 0.489;

            UpdateCategory();
        }

        // Refresh to show current database data
        private void UpdateCategory()
        {
            MovieCategory category = DatabaseFunctions.GetMovieCategory(_categoryId);
            MovieCategoryDeserialized categoryDeserialized = new MovieCategoryDeserialized(category);
            labelCategory.Content = categoryDeserialized.Name;
            icVideos.ItemsSource = categoryDeserialized.Movies;
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<MovieViewAll>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(movieViewAllWindow, 400f, 780f);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _splash.Visibility = Visibility.Collapsed;
            if (_categoryChanged)
            {
                _callback();
            }
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            Close();
        }

        // Show the movie details when a movie thumbnail is clicked
        private void movieTile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                MovieDetails popup = new MovieDetails((int)(sender as Grid).Tag, ref Splash, () => { });
                popup.scaleWindow(parentWindow);
                parentWindow.addChild(popup);
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                popup.Show();
            }
        }

        // Show info icon when hovering a thumbnail
        private void movieTile_MouseEnter(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>(sender as Grid).Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>(sender as Grid, "movieSplash").Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>(sender as Grid, "iconPlayMovie").Visibility = Visibility.Visible;
        }

        // Hide info icon when not hovering a thumbnail
        private void movieTile_MouseLeave(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>(sender as Grid).Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>(sender as Grid, "movieSplash").Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>(sender as Grid, "iconPlayMovie").Visibility = Visibility.Collapsed;
        }

        // Play the movie directly
        private void btnPlayMovie_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)(sender as Button).Tag;
            Movie movie = DatabaseFunctions.GetMovie(id);
            try
            {
                if (App.videoPlayer == null)
                {
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    try
                    {
                        VideoPlayer popup = new VideoPlayer(movie);
                        App.videoPlayer = popup;
                        popup.Width = parentWindow.ActualWidth;
                        popup.Height = parentWindow.ActualHeight;
                        popup.Owner = parentWindow;
                        popup.Left = popup.LeftMultiplier = parentWindow.Left;
                        popup.Top = popup.TopMultiplier = parentWindow.Top;
                        popup.Show();
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
                    App.videoPlayer.updateVideo(movie);
                }
            }
            catch (Exception ex)
            {
                MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                CustomMessageBox popup = new CustomMessageBox("Error: " + ex.Message, CustomMessageBox.MessageBoxType.OK);
                popup.scaleWindow(parentWindow);
                parentWindow.addChild(popup);
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                popup.ShowDialog();
                Splash.Visibility = Visibility.Collapsed;
                _callback();
            }
        }

        // Show the movie details when the details setting button is clicked
        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            MovieDetails popup = new MovieDetails((int)(sender as Button).Tag, ref Splash, () => { });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.Show();
        }

        // Delete the movie from the database
        private void btnDeleteMovie_Click(object sender, RoutedEventArgs e)
        {
            int movieId = (int)(sender as Button).Tag;
            Movie movie = DatabaseFunctions.GetMovie(movieId);

            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete " + movie.Title + " from the database? This only removes the movie from your video collection, it does not delete any movie files.", CustomMessageBox.MessageBoxType.YesNo);
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            if (popup.ShowDialog() == true)
            {
                _categoryChanged = true;
                DatabaseFunctions.DeleteMovie(movieId);
                UpdateCategory();
            }
            Splash.Visibility = Visibility.Collapsed;
        }

        // Show the update movie screen with the movie selected
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            UpdateMovie popup = new UpdateMovie(ref Splash, () => 
            { 
                _categoryChanged = true;
                UpdateCategory(); 
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            for (int i = 0; i < popup.lvMovieList.Items.Count; i++)
            {
                MovieDeserialized movie = (MovieDeserialized)popup.lvMovieList.Items[i];
                if (movie.Id.ToString() == (sender as Button).Tag.ToString())
                {
                    popup.lvMovieList.SelectedIndex = i;
                }
            }
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.Show();
        }

        // Remove the movie from the category list and the category from the list for the movie
        private void btnRemoveMovieFromCategory_Click(object sender, RoutedEventArgs e)
        {
            int movieId = (int)(sender as Button).Tag;
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                MovieCategory category = connection.Get<MovieCategory>(_categoryId);
                DatabaseFunctions.RemoveMovieFromCategory(movieId, category);

                connection.CreateTable<Movie>();
                Movie movie = connection.Get<Movie>(movieId);
                List<string> categories = JsonConvert.DeserializeObject<List<string>>(movie.Categories);
                categories.Remove(category.Name);
                movie.Categories = JsonConvert.SerializeObject(categories);
                connection.Update(movie);
            }

            _categoryChanged = true;

            UpdateCategory();
        }

        // When the size of the videos items control changes adjust padding to make it look good with a scroll bar
        private void icVideos_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Math.Round(icVideos.ActualHeight) > Math.Round(scrollVideos.ActualHeight))
            {
                icVideos.Margin = new Thickness(10, 0, 0, 0);
            } else
            {
                icVideos.Margin = new Thickness(0, 0, 0, 0);
            }
        }

        // Popup update movie category window
        private void btnUpdateCategory_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            UpdateMovieCategory popup = new UpdateMovieCategory(_categoryId, ref Splash, () =>
            {
                _categoryChanged = true;
                UpdateCategory();
            });
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.Show();
        }

        // Remove the category from the database and from all movie category lists
        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            bool deleted = false;
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                MovieCategory category = connection.Get<MovieCategory>(_categoryId);
                CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete the " + category.Name + " category?", CustomMessageBox.MessageBoxType.YesNo);
                popup.scaleWindow(parentWindow);
                parentWindow.addChild(popup);
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                if (popup.ShowDialog() == true)
                {
                    List<Movie> movies = connection.Table<Movie>().ToList();
                    foreach (Movie movie in movies)
                    {
                        List<string> categories = JsonConvert.DeserializeObject<List<string>>(movie.Categories);
                        categories.Remove(category.Name);
                        movie.Categories = JsonConvert.SerializeObject(categories);
                        connection.Update(movie);
                    }
                    connection.Delete<MovieCategory>(_categoryId);
                    deleted = true;
                }
                Splash.Visibility = Visibility.Collapsed;
            }

            if (deleted)
            {
                _splash.Visibility = Visibility.Collapsed;
                _callback();
                parentWindow.removeChild(this);
                Close();
            }
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
    }
}
