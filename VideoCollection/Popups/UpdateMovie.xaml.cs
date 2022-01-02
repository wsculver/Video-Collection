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
using Microsoft.WindowsAPICodePack.Dialogs;
using VideoCollection.Helpers;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for UpdateMovie.xaml
    /// </summary>
    public partial class UpdateMovie : Window
    {
        private List<string> _selectedCategories;
        private bool _changesSaved;
        private int _movieId;
        private MovieDeserialized _movie;
        private string _rating;

        public UpdateMovie()
        {
            InitializeComponent();

            _selectedCategories = new List<string>();
            _changesSaved = false;
            _rating = "";

            UpdateMovieList();

            // Load categories
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                List<MovieCategory> rawCategories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Name).ToList();
                List<MovieCategoryDeserialized> categories = new List<MovieCategoryDeserialized>();
                foreach (MovieCategory category in rawCategories)
                {
                    categories.Add(new MovieCategoryDeserialized(category));
                }
                icCategories.ItemsSource = categories;
            }
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

        // Select a category
        private void CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            _selectedCategories.Add(checkBox.Content.ToString());
        }

        // Unselect a category
        private void CheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            _selectedCategories.Remove(checkBox.Content.ToString());
        }

        // Close window on cancel
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // If there are changes save them before closing
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

        // Choose movie file
        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog filePath = StaticHelpers.CreateVideoFileDialog();
            if (filePath.ShowDialog() == true)
            {
                txtFile.Text = StaticHelpers.GetRelativePathStringFromCurrent(filePath.FileName);
            }
        }

        // Choose image file
        private void btnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog imagePath = StaticHelpers.CreateImageFileDialog();
            if (imagePath.ShowDialog() == true)
            {
                imgThumbnail.Source = StaticHelpers.BitmapFromUri(StaticHelpers.GetRelativePathUriFromCurrent(imagePath.FileName));
            }
        }

        // Load movie info when a movie is selected from the list
        private void lvMovieList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var movies = lvMovieList.SelectedItems;
            if (movies.Count > 0)
            {
                panelMovieInfo.Visibility = Visibility.Visible;
                _changesSaved = false;
                _selectedCategories = new List<string>();

                MovieDeserialized movie = (MovieDeserialized)movies[0];
                txtMovieFolder.Text = movie.MovieFolderPath;
                txtMovieName.Text = movie.Title;
                imgThumbnail.Source = movie.Thumbnail;
                txtFile.Text = movie.MovieFilePath;
                _movieId = movie.Id;
                _movie = movie;
                switch(movie.Rating)
                {
                    case "G":
                        btnG.IsChecked = true;
                        break;
                    case "PG":
                        btnPG.IsChecked = true;
                        break;
                    case "PG-13":
                        btnPG13.IsChecked = true;
                        break;
                    case "R":
                        btnR.IsChecked = true;
                        break;
                    case "NC-17":
                        btnNC17.IsChecked = true;
                        break;
                }
                _rating = movie.Rating;
                List<MovieCategoryDeserialized> categories = new List<MovieCategoryDeserialized>();
                foreach (MovieCategoryDeserialized category in icCategories.Items)
                {
                    bool check = false;
                    if (movie.Categories.Contains(category.Name))
                    {
                        check = true;
                        _selectedCategories.Add(category.Name);
                    }
                    categories.Add(new MovieCategoryDeserialized(category.Id, category.Position, category.Name, category.Movies, check));
                }
                icCategories.ItemsSource = categories;                
            }
        }

        // Always save changes on apply
        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            ApplyUpdate();
        }

        // Check if any movie content has changed from what was already saved
        private bool MovieContentChanged(Movie movie)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();

            return (movie.Title != txtMovieName.Text) || (movie.Thumbnail != imgThumbnail.Source.ToString()) || (movie.MovieFilePath != txtFile.Text) || (movie.Categories != jss.Serialize(_selectedCategories));
        }

        // Save changes
        private bool ApplyUpdate()
        {
            if (txtMovieFolder.Text == "")
            {
                MessageBox.Show("You need to select a movie folder", "No Movie Folder Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else if (txtMovieName.Text == "")
            {
                MessageBox.Show("You need to enter a movie name", "Missing Movie Name", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else if (txtFile.Text == "")
            {
                MessageBox.Show("You need to select a movie file", "No Movie File Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else if (_rating == "")
            {
                MessageBox.Show("You need to select a rating", "No Rating Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {

                JavaScriptSerializer jss = new JavaScriptSerializer();

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    // Update the movie in the Movie table
                    connection.CreateTable<Movie>();
                    Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + _movieId)[0];
                    bool movieContentChanged = MovieContentChanged(movie);
                    movie.Title = txtMovieName.Text.ToUpper();
                    movie.MovieFolderPath = txtMovieFolder.Text;
                    movie.Thumbnail = imgThumbnail.Source.ToString();
                    movie.MovieFilePath = txtFile.Text;
                    movie.Runtime = StaticHelpers.GetVideoDuration(txtFile.Text);
                    List<MovieBonusSection> bonusSections = new List<MovieBonusSection>();
                    foreach (MovieBonusSectionDeserialized section in _movie.BonusSections)
                    {
                        MovieBonusSection sec = new MovieBonusSection()
                        {
                            Name = section.Name,
                            Background = jss.Serialize(Color.FromArgb(0, 0, 0, 0))
                        };
                        bonusSections.Add(sec);
                    }
                    movie.BonusSections = jss.Serialize(bonusSections);
                    List<MovieBonusVideo> bonusVideos = new List<MovieBonusVideo>();
                    foreach (MovieBonusVideoDeserialized video in _movie.BonusVideos)
                    {
                        MovieBonusVideo vid = new MovieBonusVideo()
                        {
                            Title = video.Title,
                            Thumbnail = StaticHelpers.ImageSourceToBase64(video.Thumbnail),
                            FilePath = video.FilePath,
                            Section = video.Section
                        };
                        bonusVideos.Add(vid);
                    }
                    movie.BonusVideos = jss.Serialize(bonusVideos);
                    movie.Rating = _rating;
                    movie.Categories = jss.Serialize(_selectedCategories);
                    connection.Update(movie);

                    // Update the MovieCateogry table
                    connection.CreateTable<MovieCategory>();
                    List<MovieCategory> categories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Name).ToList();
                    foreach (MovieCategory category in categories)
                    {
                        if (_selectedCategories.Contains(category.Name))
                        {
                            // Update movie in the MovieCategory table if any content changed
                            if (movieContentChanged)
                            {
                                DatabaseFunctions.UpdateMovieInCategory(movie, category);
                            }
                        }
                        else
                        {
                            // Remove movie from categories in the MovieCategory table
                            DatabaseFunctions.RemoveMovieFromCategory(_movie.Id.ToString(), category);
                        }
                    }
                }
                UpdateMovieList();
                _changesSaved = true;

                return true;
            }
        }

        // Scale based on the size of the window
        private static ScaleValueHelper _scaleValueHelper = new ScaleValueHelper();
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = _scaleValueHelper.SetScaleValueProperty<UpdateMovie>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = _scaleValueHelper.CalculateScale(updateMovieWindow, 500f, 800f);
        }

        // Choose the whole movie folder
        private void btnChooseMovieFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = StaticHelpers.CreateFolderFileDialog();
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtMovieFolder.Text = StaticHelpers.GetRelativePathStringFromCurrent(dlg.FileName);
                Movie movie = StaticHelpers.ParseMovieVideos(dlg.FileName);
                _movie = new MovieDeserialized(movie);

                txtMovieName.Text = _movie.Title;
                if (movie.Thumbnail != "")
                {
                    imgThumbnail.Source = StaticHelpers.BitmapFromUri(new Uri(movie.Thumbnail));
                }
                txtFile.Text = _movie.MovieFilePath;

                if (_movie.MovieFilePath == "")
                {
                    MessageBox.Show("Warning: No movie file could be found in the folder you selected. You will have to manually select a movie file.", "No Movie File Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void RatingButtonClick(object sender, RoutedEventArgs e)
        {
            _rating = (sender as RadioButton).Content.ToString();
        }
    }
}
