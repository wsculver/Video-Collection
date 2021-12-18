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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VideoCollection.Database;
using VideoCollection.Movies;
using VideoCollection.Popups;
using VideoCollection.ViewModels;

namespace VideoCollection.Views
{
    /// <summary>
    /// Interaction logic for MoviesView.xaml
    /// </summary>
    public partial class MoviesView : UserControl
    {
        private const int _tileWidth = 276; // Tile + side margins
        private List<MovieCategoryDeserialized> _categories;

        public MoviesView()
        {
            InitializeComponent();

            DataContext = new MoviesViewModel();

            UpdateCategoryDisplay();
        }

        private void UpdateCategoryDisplay()
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                List<MovieCategory> rawCategories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Name).ToList();
                _categories = new List<MovieCategoryDeserialized>();
                foreach (MovieCategory category in rawCategories)
                {
                    _categories.Add(new MovieCategoryDeserialized(category.Id, category.Name, category.Movies, category.IsChecked));
                }
                
                icCategoryDisplay.ItemsSource = _categories;
            }
        }

        public static DependencyObject GetScrollViewer(DependencyObject o)
        {
            // Return the DependencyObject if it is a ScrollViewer
            if (o is ScrollViewer)
            { return o; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;
        }

        private void Back_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer scroll = GetScrollViewer((sender as Image).Parent) as ScrollViewer;
            double location = scroll.HorizontalOffset;

            if (location - _tileWidth < 0)
            {
                location = 0;
            }
            else
            {
                location -= _tileWidth;
            }

            scroll.ScrollToHorizontalOffset(location);
        }

        private void Next_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer scroll = GetScrollViewer((sender as Image).Parent) as ScrollViewer;
            double location = scroll.HorizontalOffset;

            if (location + _tileWidth >= scroll.ScrollableWidth)
            {
                location = scroll.ScrollableWidth;
            }
            else
            {
                location += _tileWidth;
            }

            scroll.ScrollToHorizontalOffset(location);
        }

        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            AddMovieCategory popup = new AddMovieCategory();
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();
            
            UpdateCategoryDisplay();
        }

        private void btnNewMovie_Click(object sender, RoutedEventArgs e)
        {
            AddMovie popup = new AddMovie();
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();

            UpdateCategoryDisplay();
        }

        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string categoryId = (sender as Button).Tag.ToString();
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();

                MovieCategory category = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + categoryId)[0];
                List<Movie> movies = (connection.Table<Movie>().ToList()).ToList();
                foreach(Movie movie in movies)
                {
                    List<string> categories = jss.Deserialize<List<string>>(movie.Categories);
                    categories.Remove(category.Name);
                    movie.Categories = jss.Serialize(categories);
                    connection.Update(movie);
                }
                connection.Delete<MovieCategory>(categoryId);
            }

            UpdateCategoryDisplay();
        }

        private void btnRenameCategory_Click(object sender, RoutedEventArgs e)
        {
            RenameMovieCategory popup = new RenameMovieCategory();
            popup.Owner = Window.GetWindow(this);
            popup.Tag = (sender as Button).Tag;
            popup.ShowDialog();

            UpdateCategoryDisplay();
        }

        private void btnRemoveMovieFromCategory_Click(object sender, RoutedEventArgs e)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string[] split = (sender as Button).Tag.ToString().Split(',');
            string categoryId = split[0];
            string movieId = split[1];
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                MovieCategory category = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + categoryId)[0];
                DatabaseFunctions.RemoveMovieFromCategory(movieId, category);

                connection.CreateTable<Movie>();
                Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + movieId)[0];
                List<string> categories = jss.Deserialize<List<string>>(movie.Categories);
                categories.Remove(category.Name);
                movie.Categories = jss.Serialize(categories);
                connection.Update(movie);
            }

            UpdateCategoryDisplay();
        }

        private void btnUpdateExistingMovie_Click(object sender, RoutedEventArgs e)
        {
            UpdateMovie popup = new UpdateMovie();
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();

            UpdateCategoryDisplay();
        }
    }
}
