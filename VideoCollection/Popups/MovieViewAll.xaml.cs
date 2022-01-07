using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VideoCollection.Database;
using VideoCollection.Helpers;
using VideoCollection.Movies;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for ViewAll.xaml
    /// </summary>
    public partial class MovieViewAll : Window
    {
        private string _categoryId;
        private bool _categoryChanged = false;

        // Don't use this constructur. It is only here to make resizing work
        public MovieViewAll() { }

        public MovieViewAll(string Id)
        {
            InitializeComponent();

            _categoryId = Id;

            UpdateCategory();
        }

        // Refresh to show current database data
        private void UpdateCategory()
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                MovieCategory category = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + _categoryId)[0];
                MovieCategoryDeserialized categoryDeserialized = new MovieCategoryDeserialized(category);
                labelCategory.Content = categoryDeserialized.Name;
                icVideos.ItemsSource = categoryDeserialized.Movies;
            }
        }

        // Scale based on the size of the window
        private static ScaleValueHelper _scaleValueHelper = new ScaleValueHelper();
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = _scaleValueHelper.SetScaleValueProperty<MovieViewAll>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = _scaleValueHelper.CalculateScale(movieViewAllWindow, 400f, 780f);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_categoryChanged)
            {
                DialogResult = true;
            } else
            {
                DialogResult = false;
            }
        }

        // Show the movie details when a movie thumbnail is clicked
        private void imageThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Window parentWindow = Window.GetWindow(this);
                MovieDetails popup = new MovieDetails((sender as Image).Tag.ToString());
                popup.Width = parentWindow.Width;
                popup.Height = parentWindow.Height;
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                popup.ShowDialog();
                Splash.Visibility = Visibility.Collapsed;
            }
        }

        // Show info icon when hovering a thumbnail
        private void imageThumbnail_MouseEnter(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>((sender as Image).Parent).Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "movieSplash").Visibility = Visibility.Visible;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "iconPlayMovie").Visibility = Visibility.Visible;
        }

        // Hide info icon when not hovering a thumbnail
        private void imageThumbnail_MouseLeave(object sender, MouseEventArgs e)
        {
            StaticHelpers.GetObject<Rectangle>((sender as Image).Parent).Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "movieSplash").Visibility = Visibility.Collapsed;
            StaticHelpers.GetObject<Border>((sender as Image).Parent, "iconPlayMovie").Visibility = Visibility.Collapsed;
        }

        // Play the movie directly
        private void btnPlayMovie_Click(object sender, RoutedEventArgs e)
        {

        }

        // Show the movie details when the details setting button is clicked
        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            MovieDetails popup = new MovieDetails((sender as Button).Tag.ToString());
            popup.Width = parentWindow.Width;
            popup.Height = parentWindow.Height;
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.ShowDialog();
            Splash.Visibility = Visibility.Collapsed;
        }

        // Delete the movie from the database
        private void btnDeleteMovie_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string movieId = button.Tag.ToString();
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Movie>();
                Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + movieId)[0];

                Window parentWindow = Window.GetWindow(this).Owner;
                CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete " + movie.Title + " from the database? This only removes the movie from your video collection, it does not delete any movie files.", CustomMessageBox.MessageBoxType.YesNo);
                popup.Width = parentWindow.Width * 0.25;
                popup.Height = popup.Width * 0.55;
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                if (popup.ShowDialog() == true)
                {
                    _categoryChanged = true;
                    DatabaseFunctions.DeleteMovie(movie);
                    UpdateCategory();
                }
                Splash.Visibility = Visibility.Collapsed;
            }
        }

        // Show the update movie screen with the movie selected
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Window parentWindow = Window.GetWindow(this);
            UpdateMovie popup = new UpdateMovie();
            popup.Width = parentWindow.Width * 0.67;
            popup.Height = popup.Width * 0.627;
            for (int i = 0; i < popup.lvMovieList.Items.Count; i++)
            {
                MovieDeserialized movie = (MovieDeserialized)popup.lvMovieList.Items[i];
                if (movie.Id.ToString() == button.Tag.ToString())
                {
                    popup.lvMovieList.SelectedIndex = i;
                }
            }
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            if (popup.ShowDialog() == true)
            {
                _categoryChanged = true;
                UpdateCategory();
            }
            Splash.Visibility = Visibility.Collapsed;
        }

        // Remove the movie from the category list and the category from the list for the movie
        private void btnRemoveMovieFromCategory_Click(object sender, RoutedEventArgs e)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string movieId = (sender as Button).Tag.ToString();
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                MovieCategory category = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + _categoryId)[0];
                DatabaseFunctions.RemoveMovieFromCategory(movieId, category);

                connection.CreateTable<Movie>();
                Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + movieId)[0];
                List<string> categories = jss.Deserialize<List<string>>(movie.Categories);
                categories.Remove(category.Name);
                movie.Categories = jss.Serialize(categories);
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
            Window parentWindow = Window.GetWindow(this);
            UpdateMovieCategory popup = new UpdateMovieCategory(_categoryId);
            popup.Width = parentWindow.Width * 0.35;
            popup.Height = popup.Width * 1.201;
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            if (popup.ShowDialog() == true)
            {
                _categoryChanged = true;
                UpdateCategory();
            }
            Splash.Visibility = Visibility.Collapsed;
        }

        // Remove the category from the database and from all movie category lists
        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            bool deleted = false;
            Window parentWindow = Window.GetWindow(this).Owner;
            JavaScriptSerializer jss = new JavaScriptSerializer();
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                MovieCategory category = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + _categoryId)[0];
                CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete the " + category.Name + " category?", CustomMessageBox.MessageBoxType.YesNo);
                popup.Width = parentWindow.Width * 0.25;
                popup.Height = popup.Width * 0.55;
                popup.Owner = parentWindow;
                Splash.Visibility = Visibility.Visible;
                if (popup.ShowDialog() == true)
                {
                    List<Movie> movies = connection.Table<Movie>().ToList();
                    foreach (Movie movie in movies)
                    {
                        List<string> categories = jss.Deserialize<List<string>>(movie.Categories);
                        categories.Remove(category.Name);
                        movie.Categories = jss.Serialize(categories);
                        connection.Update(movie);
                    }
                    connection.Delete<MovieCategory>(_categoryId);
                    deleted = true;
                }
                Splash.Visibility = Visibility.Collapsed;
            }

            if (deleted)
            {
                DialogResult = true;
            }
        }
    }
}
