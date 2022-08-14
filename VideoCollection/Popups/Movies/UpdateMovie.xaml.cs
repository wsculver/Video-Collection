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
using VideoCollection.Subtitles;
using Newtonsoft.Json;
using VideoCollection.CustomTypes;
using VideoCollection.Animations;
using System.Threading;
using System.Windows.Data;
using System.Collections.Concurrent;

namespace VideoCollection.Popups.Movies
{
    /// <summary>
    /// Interaction logic for UpdateMovie.xaml
    /// </summary>
    public partial class UpdateMovie : Window, ScaleableWindow
    {
        private List<string> _selectedCategories;
        private int _movieId;
        private Movie _movie;
        private string _rating;
        private string _originalMovieName;
        private Border _splash;
        private Action _callback;
        private string _thumbnailVisibility = "";
        private CancellationTokenSource _tokenSource;
        private List<MovieBonusSectionDeserialized> _movieBonusSectionsDeserialized;
        private List<MovieBonusVideoDeserialized> _movieBonusVideosDeserialized;
        private MovieDeserialized _selectedMovie = null;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public UpdateMovie() { }

        public UpdateMovie(ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _callback = callback;

            _selectedCategories = new List<string>();
            _rating = "";
            _tokenSource = new CancellationTokenSource();

            _movieBonusSectionsDeserialized = new List<MovieBonusSectionDeserialized>();
            _movieBonusVideosDeserialized = new List<MovieBonusVideoDeserialized>();

            WidthScale = 0.73;
            HeightScale = 0.85;
            HeightToWidthRatio = 0.623;

            UpdateMovieList();

            // Load categories
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                List<MovieCategory> rawCategories = connection.Table<MovieCategory>().ToList().OrderBy(c => c.Name).ToList();
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
                List<Movie> rawMovies = connection.Table<Movie>().ToList().OrderBy(c => c.Title).ToList();
                List<MovieDeserialized> movies = new List<MovieDeserialized>();
                foreach (Movie movie in rawMovies)
                {
                    try
                    {
                        movies.Add(new MovieDeserialized(movie));
                    }
                    catch (Exception ex)
                    {
                        Messages.Error(ex.Message, ref Splash);
                    }
                }
                lvMovieList.ItemsSource = movies;
            }

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvMovieList.ItemsSource);
            view.Filter = MovieFilter;
            txtFilter.IsReadOnly = false;
            txtFilter.Focusable = true;
            txtFilter.IsHitTestVisible = true;
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
            _splash.Visibility = Visibility.Collapsed;
            _callback();
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            Close();
        }

        // If there are changes save them before closing
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyUpdate())
            {
                _splash.Visibility = Visibility.Collapsed;
                _callback();
                MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                parentWindow.removeChild(this);
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
                _selectedCategories = new List<string>();

                MovieDeserialized movieDeserialized = (MovieDeserialized)movies[0];
                Movie movie = DatabaseFunctions.GetMovie(movieDeserialized.Id);
                txtMovieFolder.Text = movie.MovieFolderPath;
                txtMovieName.Text = movie.Title;
                _originalMovieName = movie.Title;
                imgThumbnail.Source = movieDeserialized.Thumbnail;
                switch (movie.ThumbnailVisibility)
                {
                    case "Visible":
                        btnImage.IsChecked = true;
                        break;
                    case "Collapsed":
                    default:
                        btnText.IsChecked = true;
                        break;
                }
                _thumbnailVisibility = movie.ThumbnailVisibility;
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
                _selectedMovie = movieDeserialized;
                setMovieBonusContent(movie);
            }
        }

        // Always save changes on apply
        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyUpdate())
            {
                int selectedMovieId = _selectedMovie.Id;
                string filterValue = txtFilter.Text;
                txtFilter.Text = "";

                UpdateMovieList();
                // Reselect the movie that is being edited
                for (int i = 0; i < lvMovieList.Items.Count; i++)
                {
                    MovieDeserialized movie = (MovieDeserialized)lvMovieList.Items[i];
                    if (movie.Id == selectedMovieId)
                    {
                        lvMovieList.SelectedIndex = i;
                    }
                }

                txtFilter.Text = filterValue;
            }
        }

        // Check if any movie content has changed from what was already saved
        private bool MovieContentChanged(Movie movie)
        {
            return (movie.Title != txtMovieName.Text)
                || (movie.MovieFolderPath != txtMovieFolder.Text)
                || (movie.Thumbnail != imgThumbnail.Source.ToString())
                || (movie.ThumbnailVisibility != _thumbnailVisibility)
                || (movie.MovieFilePath != txtFile.Text)
                || (movie.BonusSections != _movie.BonusSections)
                || (movie.BonusVideos != _movie.BonusVideos)
                || (movie.Rating != _rating)
                || (movie.Categories != JsonConvert.SerializeObject(_selectedCategories))
                || (movie.Subtitles != JsonConvert.SerializeObject(_movie.Subtitles));
        }

        // Save changes
        private bool ApplyUpdate()
        {
            bool repeat = false;
            if (_selectedMovie != null)
            {
                int selectedMovieId = _selectedMovie.Id;

                if (txtMovieFolder.Text == "")
                {
                    Messages.ShowOKMessageBox("You need to select a movie folder", ref Splash);
                    return false;
                }
                else if (txtMovieName.Text == "")
                {
                    Messages.ShowOKMessageBox("You need to enter a movie name", ref Splash);
                    return false;
                }
                else if (txtFile.Text == "")
                {
                    Messages.ShowOKMessageBox("You need to select a movie file", ref Splash);
                    return false;
                }
                else if (_thumbnailVisibility == "")
                {
                    Messages.ShowOKMessageBox("You need to select a thumbnail tile type", ref Splash);
                    return false;
                }
                else
                {
                    using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                    {
                        connection.CreateTable<Movie>();
                        List<Movie> movies = connection.Table<Movie>().ToList();
                        string movieName = txtMovieName.Text.ToUpper();
                        Parallel.ForEach(movies, m =>
                        {
                            if (m.Title != _originalMovieName && m.Title == movieName)
                            {
                                repeat = true;
                            }
                        });

                        if (repeat)
                        {
                            Messages.ShowOKMessageBox("A movie with that name already exists", ref Splash);
                        }
                        else
                        {
                            // Update the movie in the Movie table
                            Movie movie = connection.Get<Movie>(_movieId);
                            if (MovieContentChanged(movie))
                            {
                                movie.Title = txtMovieName.Text.ToUpper();
                                movie.MovieFolderPath = txtMovieFolder.Text;
                                movie.Thumbnail = StaticHelpers.ImageSourceToBase64(imgThumbnail.Source);
                                movie.ThumbnailVisibility = _thumbnailVisibility;
                                movie.MovieFilePath = txtFile.Text;
                                movie.Runtime = StaticHelpers.GetVideoDuration(txtFile.Text);
                                ConcurrentBag<MovieBonusSection> bonusSections = new ConcurrentBag<MovieBonusSection>();
                                Parallel.ForEach(_movieBonusSectionsDeserialized, section =>
                                {
                                    MovieBonusSection sec = new MovieBonusSection()
                                    {
                                        Name = section.Name,
                                        Background = JsonConvert.SerializeObject(Color.FromArgb(0, 0, 0, 0))
                                    };
                                    bonusSections.Add(sec);
                                });
                                movie.BonusSections = JsonConvert.SerializeObject(bonusSections);
                                ConcurrentBag<MovieBonusVideo> bonusVideos = new ConcurrentBag<MovieBonusVideo>();
                                Parallel.ForEach(_movieBonusVideosDeserialized, video =>
                                {
                                    MovieBonusVideo vid = new MovieBonusVideo()
                                    {
                                        Title = video.Title,
                                        Thumbnail = StaticHelpers.ImageSourceToBase64(video.Thumbnail),
                                        FilePath = video.FilePath,
                                        Section = video.Section,
                                        Runtime = video.Runtime,
                                        Subtitles = video.SubtitlesSerialized
                                    };
                                    bonusVideos.Add(vid);
                                });
                                movie.BonusVideos = JsonConvert.SerializeObject(bonusVideos);
                                movie.Rating = _rating;
                                movie.Categories = JsonConvert.SerializeObject(_selectedCategories);
                                // Parse the subtitle file
                                SubtitleParser subParser = new SubtitleParser();
                                movie.Subtitles = JsonConvert.SerializeObject(_movie.Subtitles);
                                connection.Update(movie);

                                // Update the MovieCateogry table
                                connection.CreateTable<MovieCategory>();
                                List<MovieCategory> categories = connection.Table<MovieCategory>().ToList().OrderBy(c => c.Name).ToList();
                                foreach (MovieCategory category in categories)
                                {
                                    if (!_selectedCategories.Contains(category.Name))
                                    {
                                        // Remove movie from categories in the MovieCategory table
                                        DatabaseFunctions.RemoveMovieFromCategory(_movie.Id, category);
                                    } else {
                                        DatabaseFunctions.AddMovieToCategory(_movie.Id, category);
                                    }
                                }

                                imgThumbnail.Source.Freeze();
                                App.movieThumbnails[movie.Id] = imgThumbnail.Source;
                            }
                        }
                    }                    
                }
            }

            return !repeat;
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<UpdateMovie>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(updateMovieWindow, 500f, 800f);
        }

        private void setMovieBonusContent(Movie movie)
        {
            List<MovieBonusSection> movieBonusSections = JsonConvert.DeserializeObject<List<MovieBonusSection>>(movie.BonusSections);
            _movieBonusSectionsDeserialized = new List<MovieBonusSectionDeserialized>();
            foreach (MovieBonusSection section in movieBonusSections)
            {
                _movieBonusSectionsDeserialized.Add(new MovieBonusSectionDeserialized(section));
            }
            List<MovieBonusVideo> movieBonusVideos = JsonConvert.DeserializeObject<List<MovieBonusVideo>>(movie.BonusVideos);
            _movieBonusVideosDeserialized = new List<MovieBonusVideoDeserialized>();
            foreach (MovieBonusVideo video in movieBonusVideos)
            {
                _movieBonusVideosDeserialized.Add(new MovieBonusVideoDeserialized(video));
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
                    var result = StaticHelpers.ParseMovieVideos(dlg.FileName, token);
                    if (token.IsCancellationRequested) return;
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                    {
                        if (result.IsSuccess)
                        {
                            Movie movie = result.Value;
                            try
                            {
                                setMovieBonusContent(movie);
                                _movie = movie;
                            }
                            catch (Exception ex)
                            {
                                Messages.Error(ex.Message, ref Splash);
                            }
                            txtMovieName.Text = _movie.Title;
                            if (_movie.Thumbnail != "")
                            {
                                imgThumbnail.Source = StaticHelpers.Base64ToImageSource(_movie.Thumbnail);
                            }
                            txtFile.Text = _movie.MovieFilePath;

                            if (_movie.MovieFilePath == "")
                            {
                                Messages.Warning("No movie file could be found in the folder you selected. You will have to manually select a movie file.", ref Splash);
                            }
                        }
                        else
                        {
                            txtMovieFolder.Text = "";
                            Messages.Error(result.Error, ref Splash);
                        }
                        loadingControl.Visibility = Visibility.Collapsed;
                    });
                }, token);
            }
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
            switch (option)
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

        // Delete a movie from the database
        private void btnDeleteMovie_Click(object sender, RoutedEventArgs e)
        {
            int movieId = (int)(sender as Button).Tag;
            Movie movie = DatabaseFunctions.GetMovie(movieId);

            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            CustomMessageBox popup = new CustomMessageBox("Are you sure you want to delete " + movie.Title + " from the database? This only removes the movie from your video collection, it does not delete any movie files.", CustomMessageBox.MessageBoxType.YesNo);
            popup.scaleWindow(parentWindow);
            parentWindow.addChild(popup);
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            if (popup.ShowDialog() == true)
            {
                DatabaseFunctions.DeleteMovie(movie.Id);
                UpdateMovieList();
                panelMovieInfo.Visibility = Visibility.Collapsed;
            }
            Splash.Visibility = Visibility.Collapsed;
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

        private bool MovieFilter(object item)
        {
            if (String.IsNullOrEmpty(txtFilter.Text))
                return true;
            else
                return (item as MovieDeserialized).Title.IndexOf(txtFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lvMovieList.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(lvMovieList.ItemsSource).Refresh();
            }
        }
    }
}
