using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VideoCollection.Movies;
using Microsoft.WindowsAPICodePack.Dialogs;
using VideoCollection.Helpers;
using VideoCollection.Animations;
using VideoCollection.CustomTypes;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Data;
using System.Windows.Media;
using VideoCollection.Database;

namespace VideoCollection.Popups.Movies
{
    /// <summary>
    /// Interaction logic for AddBulkMovies.xaml
    /// </summary>
    public partial class AddBulkMovies : Window, ScaleableWindow
    {
        private ConcurrentDictionary<string, Movie> _movies;
        private HashSet<string> _selectedMovieTitles;
        private Border _splash;
        private CancellationTokenSource _tokenSource;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public AddBulkMovies() { }

        public AddBulkMovies(ref Border splash)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _movies = new ConcurrentDictionary<string, Movie>();
            _selectedMovieTitles = new HashSet<string>();
            _tokenSource = new CancellationTokenSource();

            WidthScale = 0.43;
            HeightScale = 0.85;
            HeightToWidthRatio = 1.058;
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

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtRootMovieFolder.Text == "")
            {
                Messages.ShowOKMessageBox("You need to select a root movie folder", ref Splash);
            }
            else
            {
                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<Movie>();

                    foreach (KeyValuePair<string, Movie> entry in _movies)
                    {
                        if (_selectedMovieTitles.Contains(entry.Key))
                        {
                            connection.Insert(entry.Value);
                            DatabaseFunctions.AddMovieToAllCategory(entry.Value.Id, connection);
                            ImageSource thumbnail = StaticHelpers.Base64ToImageSource(entry.Value.Thumbnail);
                            thumbnail.Freeze();
                            App.movieThumbnails[entry.Value.Id] = thumbnail;
                        }
                    }
                }

                _splash.Visibility = Visibility.Collapsed;
                MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                parentWindow.removeChild(this);
                Close();
            }
        }

        // Choose the root movie folder which contains movie folders
        private void btnChooseRootMovieFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = StaticHelpers.CreateFolderFileDialog();
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtRootMovieFolder.Text = StaticHelpers.GetRelativePathStringFromCurrent(dlg.FileName);
                loadingControl.Content = new LoadingSpinner();
                loadingControl.Visibility = Visibility.Visible;
                var token = _tokenSource.Token;
                Task.Run(() => 
                {
                    var result = StaticHelpers.ParseBulkMovies(dlg.FileName, token);
                    if (token.IsCancellationRequested) return;
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                    {
                        if (result.IsFailure)
                        {
                            Messages.Error(result.Error, ref Splash, "Parse");
                            if (!result.IsPartial)
                            {
                                txtRootMovieFolder.Text = "";
                                loadingControl.Visibility = Visibility.Collapsed;
                                return;
                            }
                        }
                        _movies = result.Value;
                        if (!_movies.IsEmpty)
                        {
                            List<MovieDeserialized> movies = new List<MovieDeserialized>();
                            foreach (KeyValuePair<string, Movie> entry in _movies)
                            {
                                try
                                {
                                    MovieDeserialized movieDeserialized = new MovieDeserialized(entry.Value);
                                    movieDeserialized.IsChecked = true;
                                    movies.Add(movieDeserialized);
                                    _selectedMovieTitles.Add(entry.Key);
                                }
                                catch (Exception ex)
                                {
                                    Messages.Error(ex.Message, ref Splash);
                                }
                            }
                            movies.Sort();
                            lvMovieList.ItemsSource = movies;
                            lvMovieList.Visibility = Visibility.Visible;
                            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvMovieList.ItemsSource);
                            view.Filter = MovieFilter;
                            selectButtons.Visibility = Visibility.Visible;
                            panelAddMovies.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            txtRootMovieFolder.Text = "";
                            Messages.ShowOKMessageBox("No new movies found in that folder.", ref Splash);
                        }
                        loadingControl.Visibility = Visibility.Collapsed;
                    });
                }, token);
            }
        }

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<AddBulkMovies>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(addBulkMoviesWindow, 500f, 500f);
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

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            lvMovieList.SelectAll();
        }

        private void btnUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            lvMovieList.UnselectAll();
        }

        private void lvMovieList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedMovieTitles.Clear();
            foreach (MovieDeserialized movie in lvMovieList.SelectedItems)
            {
                _selectedMovieTitles.Add(movie.Title);
            }
        }
    }
}
