using Microsoft.Win32;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VideoCollection.Database;
using VideoCollection.Movies;
using Microsoft.WindowsAPICodePack.Dialogs;
using VideoCollection.Helpers;
using VideoCollection.Animations;
using Newtonsoft.Json;
using VideoCollection.CustomTypes;
using System.Threading;

namespace VideoCollection.Popups.Movies
{
    /// <summary>
    /// Interaction logic for AddMovie.xaml
    /// </summary>
    public partial class AddMovie : Window, ScaleableWindow
    {
        private List<MovieCategoryDeserialized> _categories;
        private List<string> _selectedCategories;
        private Movie _movie;
        private string _rating = "";
        private Border _splash;
        private Action _callback;
        private string _thumbnailVisibility = "";
        private CancellationTokenSource _tokenSource;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

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
            _tokenSource = new CancellationTokenSource();

            WidthScale = 0.43;
            HeightScale = 0.85;
            HeightToWidthRatio = 1.058;

            // Load categories to display
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                List<MovieCategory> rawCategories = connection.Table<MovieCategory>().ToList().OrderBy(c => c.Name).ToList();
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
            _tokenSource.Cancel();
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            Close();
        }

        // Shows a custom OK message box
        private void ShowOKMessageBox(string message)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            CustomMessageBox popup = new CustomMessageBox(message, CustomMessageBox.MessageBoxType.OK);
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
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
            else if (_thumbnailVisibility == "")
            {
                ShowOKMessageBox("You need to select a thumbnail tile type");
            }
            else
            {
                string thumbnail = "";
                if (imgThumbnail.Source == null)
                {
                    ImageSource image = StaticHelpers.CreateThumbnailFromVideoFile(StaticHelpers.GetAbsolutePathStringFromRelative(txtFile.Text), TimeSpan.FromSeconds(60));
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
                    ThumbnailVisibility = _thumbnailVisibility,
                    MovieFilePath = txtFile.Text,
                    Runtime = StaticHelpers.GetVideoDuration(txtFile.Text),
                    BonusSections = _movie.BonusSections,
                    BonusVideos = _movie.BonusVideos,
                    Rating = _rating,
                    Categories = JsonConvert.SerializeObject(_selectedCategories),
                    Subtitles = _movie.Subtitles
                };

                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<Movie>();
                    List<Movie> movies = connection.Table<Movie>().ToList();
                    foreach (Movie m in movies)
                    {
                        if (m.Title == txtMovieName.Text.ToUpper())
                        {
                            repeat = true;
                            break;
                        }
                    }

                    if (repeat)
                    {
                        ShowOKMessageBox("A movie with that name already exists");
                    }
                    else
                    {
                        connection.Insert(movie);

                        connection.CreateTable<MovieCategory>();
                        List<MovieCategory> categories = connection.Table<MovieCategory>().ToList().OrderBy(c => c.Name).ToList();
                        foreach (MovieCategory category in categories)
                        {
                            if (_selectedCategories.Contains(category.Name))
                            {
                                DatabaseFunctions.AddMovieToCategory(movie.Id, category);
                            }
                        }

                        imgThumbnail.Source.Freeze();
                        App.movieThumbnails[movie.Id] = imgThumbnail.Source;
                    }
                }

                if (!repeat)
                {
                    _splash.Visibility = Visibility.Collapsed;
                    _callback();
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    parentWindow.removeChild(this);
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
                var token = _tokenSource.Token;
                Task.Run(() => 
                {
                    _movie = StaticHelpers.ParseMovieVideos(dlg.FileName, token);
                    if (token.IsCancellationRequested) return;
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
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
                       });
                }, token);
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

        // Set the thumbnail tile visibility
        private void ThumbnailTileButtonClick(object sender, RoutedEventArgs e)
        {
            string option = (sender as RadioButton).Content.ToString();
            switch(option)
            {
                case "Image":
                    _thumbnailVisibility = "Visible";
                    break;
                case "Text":
                default:
                    _thumbnailVisibility = "Collapsed";
                    break;
            }
        }

        public void scaleWindow(Window parent)
        {
            Width = parent.ActualWidth * WidthScale;
            Height = Width * HeightToWidthRatio;
            if (Height > parent.ActualHeight * HeightScale)
            {
                Height = parent.ActualHeight * HeightScale;
                Width = Height / HeightToWidthRatio;
            }

            Left = parent.Left + (parent.Width - ActualWidth) / 2;
            Top = parent.Top + (parent.Height - ActualHeight) / 2;
        }
    }
}
