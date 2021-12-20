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

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for AddMovie.xaml
    /// </summary>
    public partial class AddMovie : Window
    {
        private List<MovieCategoryDeserialized> _categories;
        private List<string> _selectedCategories;

        public AddMovie()
        {
            InitializeComponent();

            _selectedCategories = new List<string>();

            // Load categories to display
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                List<MovieCategory> rawCategories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Name).ToList();
                _categories = new List<MovieCategoryDeserialized>();
                foreach (MovieCategory category in rawCategories)
                {
                    _categories.Add(new MovieCategoryDeserialized(category.Id, category.Position, category.Name, category.Movies, category.IsChecked));
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
            if (txtMovieName.Text != "")
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();

                Movie movie = new Movie()
                {
                    Title = txtMovieName.Text,
                    Thumbnail = imgThumbnail.Source.ToString(),
                    MovieFilePath = txtFile.Text,
                    BonusFolderPath = txtBonusFolder.Text,
                    Categories = jss.Serialize(_selectedCategories),
                    IsChecked = false
                };

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<Movie>();
                    connection.Insert(movie);

                    connection.CreateTable<MovieCategory>();
                    List<MovieCategory> categories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Name).ToList();
                    foreach(MovieCategory category in categories)
                    {
                        if(_selectedCategories.Contains(category.Name))
                        {
                            DatabaseFunctions.AddMovieToCategory(movie, category);
                        }
                    }
                }

                Close();
            }
            else
            {
                MessageBox.Show("You need to enter a movie name", "Missing Movie Name", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Choose movie file
        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog filePath = new OpenFileDialog();
            filePath.DefaultExt = ".m4v";
            filePath.CheckFileExists = true;
            filePath.CheckPathExists = true;
            filePath.Multiselect = false;
            filePath.ValidateNames = true;
            filePath.Filter = "Video Files|*.m4v;*.mp4;*.MOV;*.mkv";
            if (filePath.ShowDialog() == true)
            {
                txtFile.Text = filePath.FileName;
            }
        }

        // Choose image file
        private void btnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog imagePath = new OpenFileDialog();
            imagePath.DefaultExt = ".png";
            imagePath.CheckFileExists = true;
            imagePath.CheckPathExists = true;
            imagePath.Multiselect = false;
            imagePath.ValidateNames = true;
            imagePath.Filter = "Image Files|*.png;*.jpg;*.jpeg";
            if (imagePath.ShowDialog() == true)
            {
                imgThumbnail.Source = BitmapFromUri(new Uri(imagePath.FileName));
            }
        }

        // Convert a Uri into an ImageSource
        private ImageSource BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(source.AbsoluteUri);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        // Choose a folder that has bonus content
        private void btnChooseBonusFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtBonusFolder.Text = dlg.FileName;
            }
        }
    }
}
