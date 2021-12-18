using Microsoft.Win32;
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
    /// Interaction logic for UpdateMovie.xaml
    /// </summary>
    public partial class UpdateMovie : Window
    {
        private List<string> _originalCategories;
        private List<string> _selectedCategories;
        private int _movieId;
        private bool _changesSaved;

        public UpdateMovie()
        {
            InitializeComponent();

            _selectedCategories = new List<string>();
            _originalCategories = new List<string>();
            _changesSaved = false;

            UpdateMovieList();

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                List<MovieCategory> rawCategories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Name).ToList();
                List<MovieCategoryDeserialized> categories = new List<MovieCategoryDeserialized>();
                foreach (MovieCategory category in rawCategories)
                {
                    categories.Add(new MovieCategoryDeserialized(category.Id, category.Name, category.Movies, category.IsChecked));
                }
                icCategories.ItemsSource = categories;
            }
        }

        private void UpdateMovieList()
        {
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Movie>();
                List<Movie> rawMovies = (connection.Table<Movie>().ToList()).OrderBy(c => c.Title).ToList();
                List<MovieDeserialized> movies = new List<MovieDeserialized>();
                foreach (Movie movie in rawMovies)
                {
                    movies.Add(new MovieDeserialized(movie.Id, movie.Title, movie.Thumbnail, movie.MovieFilePath, movie.Categories));
                }
                lvMovieList.ItemsSource = movies;
            }
        }

        private void CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            _selectedCategories.Add(checkBox.Content.ToString());
        }

        private void CheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            _selectedCategories.Remove(checkBox.Content.ToString());
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (!_changesSaved)
            {
                if (ApplyUpdate())
                {
                    Close();
                }
            }
            else
            {
                Close();
            }
        }

        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog filePath = new OpenFileDialog();
            if (filePath.ShowDialog() == true)
            {
                txtFile.Text = filePath.FileName;
            }
        }

        private void btnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog imagePath = new OpenFileDialog();
            imagePath.DefaultExt = ".png";
            imagePath.Filter = "png Files (*.png)|*.png|jpeg Files (*.jpg)|*.jpg";
            if (imagePath.ShowDialog() == true)
            {
                imgThumbnail.Source = BitmapFromUri(new Uri(imagePath.FileName));
            }
        }

        private ImageSource BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(source.AbsoluteUri);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        private void lvMovieList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var movies = lvMovieList.SelectedItems;
            if (movies.Count > 0)
            {
                _changesSaved = false;
                _selectedCategories = new List<string>();
                _originalCategories = new List<string>();

                MovieDeserialized movie = (MovieDeserialized)movies[0];
                txtMovieName.Text = movie.Title;
                imgThumbnail.Source = movie.Thumbnail;
                txtFile.Text = movie.MovieFilePath;
                _movieId = movie.Id;
                List<MovieCategoryDeserialized> categories = new List<MovieCategoryDeserialized>();
                foreach (MovieCategoryDeserialized category in icCategories.Items)
                {
                    bool check = false;
                    if (movie.Categories.Contains(category.Name))
                    {
                        check = true;
                        _selectedCategories.Add(category.Name);
                        _originalCategories.Add(category.Name);
                    }
                    categories.Add(new MovieCategoryDeserialized(category.Id, category.Name, category.Movies, check));
                }
                icCategories.ItemsSource = categories;
            }
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            ApplyUpdate();
        }

        private bool ApplyUpdate()
        {
            if (txtMovieName.Text != "")
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    // Update the movie in the Movie table
                    connection.CreateTable<Movie>();
                    Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + _movieId)[0];
                    movie.Title = txtMovieName.Text;
                    movie.Thumbnail = imgThumbnail.Source.ToString();
                    movie.MovieFilePath = txtFile.Text;
                    movie.Categories = jss.Serialize(_selectedCategories);
                    connection.Update(movie);

                    // Update the MovieCateogry table
                    connection.CreateTable<MovieCategory>();
                    List<MovieCategory> categories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Name).ToList();
                    foreach (MovieCategory category in categories)
                    {
                        if (_selectedCategories.Contains(category.Name))
                        {
                            // Add movie to categories in the MovieCategory table
                            if (!_originalCategories.Contains(category.Name))
                            {
                                DatabaseFunctions.AddMovieToCategory(movie, category);
                            }
                        }
                        else
                        {
                            // Remove movie from categories in the MovieCategory table
                            DatabaseFunctions.RemoveMovieFromCategory(_movieId.ToString(), category);
                        }
                    }
                }
                UpdateMovieList();
                _changesSaved = true;

                return true;
            }
            else
            {
                MessageBox.Show("You need to enter a movie name", "Missing Movie Name", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }
    }
}
