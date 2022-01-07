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
using VideoCollection.Helpers;
using VideoCollection.Movies;
using VideoCollection.Views;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for AddMovieCategory.xaml
    /// </summary>
    public partial class AddMovieCategory : Window
    {
        private List<int> _selectedMovieIds;

        public AddMovieCategory()
        {
            InitializeComponent();

            _selectedMovieIds = new List<int>();

            UpdateMovieList();
        }

        // Load current movie list
        private void UpdateMovieList()
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Movie>();
                List<Movie> rawMovies = (connection.Table<Movie>().ToList()).OrderBy(c => c.Title).ToList();
                List<MovieDeserialized> movies = new List<MovieDeserialized>();
                foreach (Movie movie in rawMovies)
                {
                    movies.Add(new MovieDeserialized(movie));
                }
                lvMovieList.ItemsSource = movies;
            }
        }

        // Close window on cancel
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // Shows a custom OK message box
        private void ShowOKMessageBox(string message)
        {
            Window parentWindow = GetWindow(this).Owner;
            CustomMessageBox popup = new CustomMessageBox(message, CustomMessageBox.MessageBoxType.OK);
            popup.Width = parentWindow.Width * 0.25;
            popup.Height = popup.Width * 0.55;
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.ShowDialog();
            Splash.Visibility = Visibility.Collapsed;
        }

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtCategoryName.Text == "")
            {
                ShowOKMessageBox("You need to enter a category name");
            }
            else 
            { 
                JavaScriptSerializer jss = new JavaScriptSerializer();

                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<MovieCategory>();
                    List<MovieCategory> categories = connection.Table<MovieCategory>().ToList();
                    foreach (MovieCategory movieCategory in categories)
                    {
                        if (movieCategory.Name == txtCategoryName.Text.ToUpper())
                            repeat = true;
                    }

                    if (repeat)
                    {
                        ShowOKMessageBox("A category with that name already exists");
                    }
                    else
                    {
                        List<Movie> selectedMovies = new List<Movie>();
                        connection.CreateTable<Movie>();
                        foreach (int id in _selectedMovieIds)
                        {
                            Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + id.ToString())[0];
                            selectedMovies.Add(movie);

                            // Add category to selected movie
                            List<string> movieCategories = jss.Deserialize<List<string>>(movie.Categories);
                            movieCategories.Add(txtCategoryName.Text.ToUpper());
                            movie.Categories = jss.Serialize(movieCategories);
                            connection.Update(movie);
                        }

                        selectedMovies.Sort();

                        MovieCategory category = new MovieCategory()
                        {
                            Name = txtCategoryName.Text.ToUpper(),
                            Movies = jss.Serialize(selectedMovies),
                            IsChecked = false
                        };

                        connection.CreateTable<MovieCategory>();
                        connection.Query<MovieCategory>("CREATE TRIGGER IF NOT EXISTS updatePosition AFTER INSERT ON MovieCategory BEGIN UPDATE MovieCategory SET Position = new.Id WHERE Id = new.Id; END; ");
                        connection.Insert(category);
                    }
                }

                if (!repeat)
                {
                    DialogResult = true;
                }
            }
        }


        // Select a movie
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _selectedMovieIds.Add((int)(sender as CheckBox).Tag);
        }

        // Unselect a movie
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _selectedMovieIds.Remove((int)(sender as CheckBox).Tag);
        }

        // Scale based on the size of the window
        private static ScaleValueHelper _scaleValueHelper = new ScaleValueHelper();
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = _scaleValueHelper.SetScaleValueProperty<AddMovieCategory>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = _scaleValueHelper.CalculateScale(addMovieCategoryWindow, 500f, 350f);
        }
    }
}
