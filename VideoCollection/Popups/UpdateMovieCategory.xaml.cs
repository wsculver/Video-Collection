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
using VideoCollection.Movies;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for RenameMovieCategory.xaml
    /// </summary>
    public partial class UpdateMovieCategory : Window
    {
        private List<int> _selectedMovieIds;

        public UpdateMovieCategory(string Id)
        {
            InitializeComponent();

            _selectedMovieIds = new List<int>();

            Tag = Id;

            // Check current movies and fill the category name text box
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                MovieCategory movieCategory = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + Tag.ToString())[0];
                txtCategoryName.Text = movieCategory.Name;
                MovieCategoryDeserialized movieCategoryDeserialized = new MovieCategoryDeserialized(movieCategory.Id, movieCategory.Position, movieCategory.Name, movieCategory.Movies, movieCategory.IsChecked);

                connection.CreateTable<Movie>();
                List<Movie> rawMovies = (connection.Table<Movie>().ToList()).OrderBy(c => c.Title).ToList();
                List<MovieDeserialized> movies = new List<MovieDeserialized>();
                foreach (Movie movie in rawMovies)
                {
                    bool check = false;
                    foreach (Movie movieDeserialized in movieCategoryDeserialized.Movies)
                    {
                        if (movieDeserialized.Id == movie.Id)
                        {
                            check = true;
                            _selectedMovieIds.Add(movie.Id);
                        }
                    }

                    movies.Add(new MovieDeserialized(movie.Id, movie.Title, movie.Thumbnail, movie.MovieFilePath, movie.Categories, check));
                }
                lvMovieList.ItemsSource = movies;
            }
        }

        // Close the window on cancel
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtCategoryName.Text != "")
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    List<Movie> selectedMovies = new List<Movie>();

                    foreach (int id in _selectedMovieIds)
                    {
                        Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + id.ToString())[0];
                        selectedMovies.Add(movie);
                    }

                    MovieCategory result = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + Tag.ToString())[0];
                    DatabaseFunctions.UpdateCategoryNameInMovies(result.Name, txtCategoryName.Text.ToUpper());
                    result.Name = txtCategoryName.Text.ToUpper();
                    result.Movies = jss.Serialize(selectedMovies);
                    connection.Update(result);
                }

                Close();
            }
            else
            {
                MessageBox.Show("You need to enter a category name", "Missing Category Name", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Add movie to selected
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _selectedMovieIds.Add((int)(sender as CheckBox).Tag);
        }

        // Remove movie from selected
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _selectedMovieIds.Remove((int)(sender as CheckBox).Tag);
        }
    }
}
