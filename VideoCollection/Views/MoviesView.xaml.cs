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
        private const int _tileWidth = 284; // Tile + side margins
        private List<MovieCategoryDeserialized> _categories;

        public MoviesView()
        {
            InitializeComponent();

            DataContext = new MoviesViewModel();

            UpdateCategoryDisplay();
        }

        // Refresh category display to show current database data
        private void UpdateCategoryDisplay()
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                List<MovieCategory> rawCategories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Position).ToList();
                _categories = new List<MovieCategoryDeserialized>();
                foreach (MovieCategory category in rawCategories)
                {
                    _categories.Add(new MovieCategoryDeserialized(category.Id, category.Position, category.Name, category.Movies, category.IsChecked));
                }
                
                icCategoryDisplay.ItemsSource = _categories;
            }

            UpdateCategoryScrollButtons();
        }

        // Popup add category window
        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            AddMovieCategory popup = new AddMovieCategory();
            popup.Width = Window.GetWindow(this).Width * 0.2;
            popup.Height = Window.GetWindow(this).Height * 0.55;
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();
            
            UpdateCategoryDisplay();
        }

        // Popup add movie window
        private void btnNewMovie_Click(object sender, RoutedEventArgs e)
        {
            AddMovie popup = new AddMovie();
            popup.Width = Window.GetWindow(this).Width * 0.3;
            popup.Height = Window.GetWindow(this).Height * 0.6;
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();

            UpdateCategoryDisplay();
        }

        // Remove the category from the database and from all movie category lists
        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string categoryId = (sender as Button).Tag.ToString();
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();

                MovieCategory category = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + categoryId)[0];
                List<Movie> movies = connection.Table<Movie>().ToList();
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

        // Popup update movie category window
        private void btnUpdateCategory_Click(object sender, RoutedEventArgs e)
        {
            UpdateMovieCategory popup = new UpdateMovieCategory((sender as Button).Tag.ToString());
            popup.Width = Window.GetWindow(this).Width * 0.2;
            popup.Height = Window.GetWindow(this).Height * 0.6;
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();

            UpdateCategoryDisplay();
        }

        // Remove the movie from the category list and the category from the list for the movie
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

        // Popup update movie window
        private void btnUpdateExistingMovie_Click(object sender, RoutedEventArgs e)
        {
            UpdateMovie popup = new UpdateMovie();
            popup.Width = Window.GetWindow(this).Width * 0.5;
            popup.Height = Window.GetWindow(this).Height * 0.6;
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();

            UpdateCategoryDisplay();
        }

        // Shift a category up by one
        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            int categoryId = (int)(sender as Button).Tag;
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();

                List<MovieCategory> categories = connection.Table<MovieCategory>().ToList().OrderBy(c => c.Position).ToList();

                MovieCategory previousCategory = categories[0];
                foreach(MovieCategory category in categories)
                {
                    if(category.Id == categoryId)
                    {
                        if (category != previousCategory)
                        {
                            category.Position -= 1;
                            previousCategory.Position += 1;
                            connection.Update(category);
                            connection.Update(previousCategory);
                        }
                        break;
                    }
                    previousCategory = category;
                }
            }

            UpdateCategoryDisplay();
        }

        // Shift a category down by one
        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            int categoryId = (int)(sender as Button).Tag;
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();

                List<MovieCategory> categories = connection.Table<MovieCategory>().ToList().OrderByDescending(c => c.Position).ToList();

                MovieCategory previousCategory = categories[0];
                foreach (MovieCategory category in categories)
                {
                    if (category.Id == categoryId)
                    {
                        if (category != previousCategory)
                        {
                            category.Position += 1;
                            previousCategory.Position -= 1;
                            connection.Update(category);
                            connection.Update(previousCategory);
                        }
                        break;
                    }
                    previousCategory = category;
                }
            }

            UpdateCategoryDisplay();
        }

        // Left arrow button to scroll left inside a category
        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button);
            Image tile = GetObject<Image>(button.Parent) as Image;
            double tileWidth = 0;
            if (tile != null) 
            {
                tileWidth = tile.ActualWidth + tile.Margin.Left + tile.Margin.Right;
            }
            ScrollViewer scroll = GetObject<ScrollViewer>(button.Parent) as ScrollViewer;
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

        // Right arrow button to scroll right inside a category
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button);
            Image tile = GetObject<Image>(button.Parent) as Image;
            double tileWidth = 0;
            if (tile != null)
            {
                tileWidth = tile.ActualWidth + tile.Margin.Left + tile.Margin.Right;
            }
            ScrollViewer scroll = GetObject<ScrollViewer>(button.Parent) as ScrollViewer;
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

        // Get the previous button for a category
        public static DependencyObject GetPreviousButton(DependencyObject o)
        {
            // Return the DependencyObject if it is a Button with the name btnPrevious
            if (o is Button && (o as Button).Name == "btnPrevious")
            { return o; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetPreviousButton(child);
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

        // Get the next button for a category
        public static DependencyObject GetNextButton(DependencyObject o)
        {
            // Return the DependencyObject if it is a Button with the name btnNext
            if (o is Button && (o as Button).Name == "btnNext")
            { return o; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetNextButton(child);
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

        // Prevent background scrolling from stopping when the mouse is over a category
        private void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer && !e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        // Show the movie details when a movie thumbnail is clicked
        private void imageThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MovieDetails popup = new MovieDetails((sender as Image).Tag.ToString());
            popup.Width = Window.GetWindow(this).Width * 0.95;
            popup.Height = Window.GetWindow(this).Height * 0.9;
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();
        }

        // When the grid size is changed update the category scroll buttons
        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCategoryScrollButtons();
        }

        // Make the category scroll buttons visable if they are needed
        private void UpdateCategoryScrollButtons()
        {
            for (int i = 0; i < icCategoryDisplay.Items.Count; i++)
            {
                ContentPresenter c = (ContentPresenter)icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i);
                if (c != null)
                {
                    c.ApplyTemplate();
                    ItemsControl moviesControl = c.ContentTemplate.FindName("icMovies", c) as ItemsControl;
                    ContentPresenter c2 = (ContentPresenter)moviesControl.ItemContainerGenerator.ContainerFromIndex(0);
                    if (c2 != null)
                    {
                        c2.ApplyTemplate();
                        Image tile = c2.ContentTemplate.FindName("imageThumbnail", c2) as Image;
                        ScrollViewer scrollViewer = c.ContentTemplate.FindName("scrollMovies", c) as ScrollViewer;
                        if (Math.Round(moviesControl.Items.Count * (tile.ActualWidth + tile.Margin.Left + tile.Margin.Right)) > Math.Round(scrollViewer.ActualWidth) && scrollViewer.HorizontalOffset < scrollViewer.ScrollableWidth)
                        {
                            (GetNextButton(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i)) as Button).Visibility = Visibility.Visible;
                        }
                        else
                        {
                            (GetNextButton(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i)) as Button).Visibility = Visibility.Hidden;
                        }

                        if (scrollViewer.HorizontalOffset > 0)
                        {
                            (GetPreviousButton(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i)) as Button).Visibility = Visibility.Visible;
                        }
                        else
                        {
                            (GetPreviousButton(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i)) as Button).Visibility = Visibility.Hidden;
                        }
                    }
                }
            }
        }

        // Get the first child of type T
        public static DependencyObject GetObject<T>(DependencyObject o)
        {
            if (o is T)
            { return o; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetObject<T>(child);
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

        private void scrollMovies_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateCategoryScrollButtons();
        }
    }
}
