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
using System.IO;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for AddMovie.xaml
    /// </summary>
    public partial class AddMovie : Window
    {
        private List<MovieCategoryDeserialized> _categories;
        private List<string> _selectedCategories;
        private Movie _movie;

        public AddMovie()
        {
            InitializeComponent();

            _selectedCategories = new List<string>();
            _movie = new Movie();

            // Load categories to display
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                List<MovieCategory> rawCategories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Name).ToList();
                _categories = new List<MovieCategoryDeserialized>();
                foreach (MovieCategory category in rawCategories)
                {
                    _categories.Add(new MovieCategoryDeserialized(category));
                }
                icCategories.ItemsSource = _categories;
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

        // Close the window on cancel
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtMovieFolder.Text == "")
            {
                MessageBox.Show("You need to select a movie folder", "No Movie Folder Selected", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (txtMovieName.Text == "")
            {
                MessageBox.Show("You need to enter a movie name", "Missing Movie Name", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (txtFile.Text == "")
            {
                MessageBox.Show("You need to select a movie file", "No Movie File Selected", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();

                string thumbnail = "";
                if (imgThumbnail.Source == null)
                {
                    thumbnail = StaticHelpers.CreateThumbnailFromVideoFile(txtMovieFolder.Text, txtFile.Text, 60);
                }
                else
                {
                    thumbnail = imgThumbnail.Source.ToString();
                }

                Movie movie = new Movie()
                {
                    Title = txtMovieName.Text.ToUpper(),
                    MovieFolderPath = txtMovieFolder.Text,
                    Thumbnail = thumbnail,
                    MovieFilePath = txtFile.Text,
                    BonusSections = _movie.BonusSections,
                    BonusVideos = _movie.BonusVideos,
                    Categories = jss.Serialize(_selectedCategories),
                    IsChecked = false
                };

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<Movie>();
                    connection.Insert(movie);

                    connection.CreateTable<MovieCategory>();
                    List<MovieCategory> categories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Name).ToList();
                    foreach (MovieCategory category in categories)
                    {
                        if (_selectedCategories.Contains(category.Name))
                        {
                            DatabaseFunctions.AddMovieToCategory(movie, category);
                        }
                    }
                }

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

        // Choose the whole movie folder
        private void btnChooseMovieFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = StaticHelpers.CreateFolderFileDialog();
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtMovieFolder.Text = StaticHelpers.GetRelativePathStringFromCurrent(dlg.FileName);
                _movie = StaticHelpers.ParseMovieVideos(dlg.FileName);

                txtMovieName.Text = _movie.Title;
                if (_movie.Thumbnail != "")
                {
                    imgThumbnail.Source = StaticHelpers.BitmapFromUri(new Uri(_movie.Thumbnail));
                }
                txtFile.Text = _movie.MovieFilePath;

                panelMovieFields.Visibility = Visibility.Visible;

                if(_movie.MovieFilePath == "")
                {
                    MessageBox.Show("Warning: No movie file could be found in the folder you selected. You will have to manually select a movie file.", "No Movie File Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Scale based on the size of the window
        private static ScaleValueHelper _scaleValueHelper = new ScaleValueHelper();
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = _scaleValueHelper.SetScaleValueProperty<AddMovie>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = _scaleValueHelper.CalculateScale(addMovieWindow, 500f, 500f);
        }
    }
}
