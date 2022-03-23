using Microsoft.Win32;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using VideoCollection.Subtitles;
using System.Drawing.Imaging;
using VideoCollection.Animations;
using Newtonsoft.Json;

namespace VideoCollection.Popups.Movies
{
    /// <summary>
    /// Interaction logic for AddMovie.xaml
    /// </summary>
    public partial class AddMovie : Window
    {
        private List<MovieCategoryDeserialized> _categories;
        private List<string> _selectedCategories;
        private Movie _movie;
        private string _rating = "";
        private Border _splash;
        private Action _callback;

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public AddMovie() { }

        public AddMovie(ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _callback = callback;
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
            _splash.Visibility = Visibility.Collapsed;
            Close();
        }

        // Shows a custom OK message box
        private void ShowOKMessageBox(string message)
        {
            Window parentWindow = GetWindow(this).Owner;
            CustomMessageBox popup = new CustomMessageBox(message, CustomMessageBox.MessageBoxType.OK);
            popup.Width = parentWindow.ActualWidth * 0.25;
            popup.Height = popup.Width * 0.55;
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.ShowDialog();
            Splash.Visibility = Visibility.Collapsed;
        }

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtMovieFolder.Text == "")
            {
                ShowOKMessageBox("You need to select a movie folder");
            }
            else if (txtMovieName.Text == "")
            {
                ShowOKMessageBox("You need to enter a movie name");
            }
            else if (txtFile.Text == "")
            { 
                ShowOKMessageBox("You need to select a movie file");
            }
            else if (_rating == "")
            {
                ShowOKMessageBox("You need to select a rating");
            }
            else
            {
                string thumbnail = "";
                if (imgThumbnail.Source == null)
                {
                    ImageSource image = StaticHelpers.CreateThumbnailFromVideoFile(txtFile.Text, TimeSpan.FromSeconds(60));
                    thumbnail = StaticHelpers.ImageSourceToBase64(image);
                }
                else
                {
                    thumbnail = StaticHelpers.ImageSourceToBase64(imgThumbnail.Source);
                }

                Movie movie = new Movie()
                {
                    Title = txtMovieName.Text.ToUpper(),
                    MovieFolderPath = txtMovieFolder.Text,
                    Thumbnail = thumbnail,
                    MovieFilePath = txtFile.Text,
                    Runtime = StaticHelpers.GetVideoDuration(txtFile.Text),
                    BonusSections = _movie.BonusSections,
                    BonusVideos = _movie.BonusVideos,
                    Rating = _rating,
                    Categories = JsonConvert.SerializeObject(_selectedCategories),
                    Subtitles = _movie.Subtitles,
                    IsChecked = false
                };

                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<Movie>();
                    List<Movie> movies = connection.Table<Movie>().ToList();
                    foreach (Movie m in movies)
                    {
                        if (m.Title == txtMovieName.Text.ToUpper())
                            repeat = true;
                    }

                    if (repeat)
                    {
                        ShowOKMessageBox("A movie with that name already exists");
                    }
                    else
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
                }

                if (!repeat)
                {
                    _splash.Visibility = Visibility.Collapsed;
                    _callback();
                    Close();
                }
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
                loadingControl.Content = new LoadingSpinner();
                loadingControl.Visibility = Visibility.Visible;
                Task.Run(async () =>
                {
                    _movie = await StaticHelpers.ParseMovieVideos(dlg.FileName);
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                       (Action)(() =>
                       {
                           txtMovieName.Text = _movie.Title;
                           if (_movie.Thumbnail != "")
                           {
                               imgThumbnail.Source = StaticHelpers.Base64ToImageSource(_movie.Thumbnail);
                           }
                           txtFile.Text = _movie.MovieFilePath;

                           panelMovieFields.Visibility = Visibility.Visible;

                           if (_movie.MovieFilePath == "")
                           {
                               ShowOKMessageBox("Warning: No movie file could be found in the folder you selected. You will have to manually select a movie file.");
                           }
                           loadingControl.Visibility = Visibility.Collapsed;
                       }));
                });
            }
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<AddMovie>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(addMovieWindow, 500f, 500f);
        }

        // Set the movie rating
        private void RatingButtonClick(object sender, RoutedEventArgs e)
        {
            _rating = (sender as RadioButton).Content.ToString();
        }
    }
}
