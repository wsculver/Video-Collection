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
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VideoCollection.CustomTypes;
using VideoCollection.Database;
using VideoCollection.Helpers;
using VideoCollection.Movies;
using VideoCollection.Popups;
using VideoCollection.Popups.Movies;

namespace VideoCollection.Views
{
    /// <summary>
    /// Interaction logic for MoviesView.xaml
    /// </summary>
    public partial class MoviesView : UserControl
    {
        private const int _sideMargins = 24;
        private const int _scrollViewerMargins = 24;
        private double _scrollDistance = 0;
        private List<MovieCategoryDeserialized> _categories;

        public MoviesView()
        {
            InitializeComponent();

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
                    _categories.Add(new MovieCategoryDeserialized(category));
                }
                
                icCategoryDisplay.ItemsSource = _categories;
            }

            UpdateCategoryScrollButtons();
        }        

        // Popup add category window
        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
            AddMovieCategory popup = new AddMovieCategory(ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.Width = parentWindow.ActualWidth * 0.35;
            popup.Height = popup.Width * 1.299;
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
        }

        // Popup add movie window
        private void btnNewMovie_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
            AddMovie popup = new AddMovie(ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.Width = parentWindow.ActualWidth * 0.43;
            popup.Height = popup.Width * 1.058;
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
        }

        // Remove the category from the database and from all movie category lists
        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            bool deleted = false;
            MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string categoryId = (sender as Button).Tag.ToString();
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                MovieCategory category = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + categoryId)[0];
                CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete the " + category.Name + " category?", CustomMessageBox.MessageBoxType.YesNo);
                popup.Width = parentWindow.ActualWidth * 0.25;
                popup.Height = popup.Width * 0.55;
                popup.Owner = parentWindow;
                parentWindow.Splash.Visibility = Visibility.Visible;
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
                    connection.Delete<MovieCategory>(categoryId);
                    deleted = true;
                }
                parentWindow.Splash.Visibility = Visibility.Collapsed;
            }

            if (deleted)
            {
                UpdateCategoryDisplay();
            }
        }

        // Popup update movie category window
        private void btnUpdateCategory_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
            UpdateMovieCategory popup = new UpdateMovieCategory((sender as Button).Tag.ToString(), ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.Width = parentWindow.ActualWidth * 0.35;
            popup.Height = popup.Width * 1.299;
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
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
            MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
            UpdateMovie popup = new UpdateMovie(ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.Width = parentWindow.ActualWidth * 0.73;
            popup.Height = popup.Width * 0.623;
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
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

        // Right arrow button to scroll right inside a category
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
            if (e.ChangedButton == MouseButton.Left)
            {
                MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
                MovieDetails popup = new MovieDetails((sender as Image).Tag.ToString(), ref parentWindow.Splash, () => { });
                popup.Width = parentWindow.ActualWidth * 0.93;
                popup.Height = popup.Width * 0.489;
                popup.Owner = parentWindow;
                parentWindow.Splash.Visibility = Visibility.Visible;
                popup.Show();
            }
        }

        // When the grid size is changed scale the middle column to fit as many tiles as possible
        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double columnWidth = 0;

            for (int i = 0; i < icCategoryDisplay.Items.Count; i++)
            {
                ContentPresenter c = (ContentPresenter)icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i);
                if (c != null)
                {
                    c.ApplyTemplate();
                    ColumnDefinition middleColumn = c.ContentTemplate.FindName("colMiddle", c) as ColumnDefinition;

                    if (columnWidth == 0)
                    {
                        columnWidth = _scrollViewerMargins;
                        double tileWidth = 142;
                        while(columnWidth + tileWidth + _sideMargins < MainGrid.ActualWidth)
                        {
                            columnWidth += tileWidth;
                        }
                    }
                    
                    middleColumn.Width = new GridLength(columnWidth);
                }
            }

            _scrollDistance = columnWidth - _scrollViewerMargins;

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
                        bool needScroll = Math.Round(moviesControl.Items.Count * (tile.ActualWidth + tile.Margin.Left + tile.Margin.Right)) > Math.Round(scrollViewer.ActualWidth);
                        // Show view all button or not
                        if (needScroll)
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnViewAll").Visibility = Visibility.Visible;
                        } 
                        else
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnViewAll").Visibility = Visibility.Hidden;
                        }

                        // Show next button or not
                        if (needScroll && scrollViewer.HorizontalOffset < scrollViewer.ScrollableWidth)
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnNext").Visibility = Visibility.Visible;
                        }
                        else
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnNext").Visibility = Visibility.Hidden;
                        }

                        // Show previous button or not
                        if (scrollViewer.HorizontalOffset > 0)
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnPrevious").Visibility = Visibility.Visible;
                        }
                        else
                        {
                            StaticHelpers.GetObject<Button>(icCategoryDisplay.ItemContainerGenerator.ContainerFromIndex(i), "btnPrevious").Visibility = Visibility.Hidden;
                        }
                    }
                }
            }
        }

        private void scrollMovies_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateCategoryScrollButtons();
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
            string id = (sender as Button).Tag.ToString();
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Movie>();
                Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + id)[0];
                try
                {
                    MovieDeserialized movieDeserialized = new MovieDeserialized(movie);
                    if (App.videoPlayer == null)
                    {
                        Window parentWindow = Application.Current.MainWindow;
                        try
                        {
                            VideoPlayer popup = new VideoPlayer(movieDeserialized);
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
                            popup.Width = parentWindow.ActualWidth * 0.25;
                            popup.Height = popup.Width * 0.55;
                            popup.Owner = parentWindow;
                            popup.ShowDialog();
                        }
                    }
                    else
                    {
                        App.videoPlayer.updateVideo(movieDeserialized);
                    }
                }
                catch (Exception ex)
                {
                    MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
                    CustomMessageBox popup = new CustomMessageBox("Error: " + ex.Message + ".", CustomMessageBox.MessageBoxType.OK);
                    popup.Width = parentWindow.ActualWidth * 0.25;
                    popup.Height = popup.Width * 0.55;
                    popup.Owner = parentWindow;
                    parentWindow.Splash.Visibility = Visibility.Visible;
                    popup.ShowDialog();
                    parentWindow.Splash.Visibility = Visibility.Collapsed;
                }
            }
        }

        // Show the movie details when the details setting button is clicked
        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
            MovieDetails popup = new MovieDetails((sender as Button).Tag.ToString(), ref parentWindow.Splash, () => { });
            popup.Width = parentWindow.ActualWidth * 0.93;
            popup.Height = popup.Width * 0.489;
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
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

                MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
                CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete " + movie.Title + " from the database? This only removes the movie from your video collection, it does not delete any movie files.", CustomMessageBox.MessageBoxType.YesNo);
                popup.Width = parentWindow.ActualWidth * 0.25;
                popup.Height = popup.Width * 0.55;
                popup.Owner = parentWindow;
                parentWindow.Splash.Visibility = Visibility.Visible;
                if (popup.ShowDialog() == true)
                {
                    DatabaseFunctions.DeleteMovie(movie);
                    UpdateCategoryDisplay();
                }
                parentWindow.Splash.Visibility = Visibility.Collapsed;
            }
        }

        // Show the update movie screen with the movie selected
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
            UpdateMovie popup = new UpdateMovie(ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.Width = parentWindow.ActualWidth * 0.73;
            popup.Height = popup.Width * 0.623;
            for (int i = 0; i < popup.lvMovieList.Items.Count; i++)
            {
                MovieDeserialized movie = (MovieDeserialized)popup.lvMovieList.Items[i];
                if (movie.Id.ToString() == button.Tag.ToString())
                {
                    popup.lvMovieList.SelectedIndex = i;
                }
            }
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
        }

        // Popup a window showing all movies in the cateogry
        private void btnViewAll_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            MainWindow parentWindow = (MainWindow)Window.GetWindow(this);
            MovieViewAll popup = new MovieViewAll(button.Tag.ToString(), ref parentWindow.Splash, () =>
            {
                UpdateCategoryDisplay();
            });
            popup.Width = parentWindow.ActualWidth * 0.93;
            popup.Height = popup.Width * 0.489;
            popup.Owner = parentWindow;
            parentWindow.Splash.Visibility = Visibility.Visible;
            popup.Show();
        }
    }
}
