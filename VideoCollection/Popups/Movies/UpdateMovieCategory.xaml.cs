using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using VideoCollection.Database;
using VideoCollection.Helpers;
using VideoCollection.Movies;
using VideoCollection.CustomTypes;
using System.Windows.Data;

namespace VideoCollection.Popups.Movies
{
    /// <summary>
    /// Interaction logic for RenameMovieCategory.xaml
    /// </summary>
    public partial class UpdateMovieCategory : Window, ScaleableWindow
    {
        private List<int> _selectedMovieIds;
        private string _originalCategoryName;
        private Border _splash;
        private Action _callback;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public UpdateMovieCategory() { }

        public UpdateMovieCategory(int id, ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _selectedMovieIds = new List<int>();

            Tag = id;
            _splash = splash;
            _callback = callback;

            WidthScale = 0.35;
            HeightScale = 0.85;
            HeightToWidthRatio = 1.299;

            // Check current movies and fill the category name text box
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                MovieCategory movieCategory = connection.Get<MovieCategory>(id);
                txtCategoryName.Text = movieCategory.Name;
                _originalCategoryName = movieCategory.Name;
                MovieCategoryDeserialized movieCategoryDeserialized = new MovieCategoryDeserialized(movieCategory);

                connection.CreateTable<Movie>();
                List<Movie> rawMovies = connection.Table<Movie>().ToList();
                List<MovieDeserialized> movies = new List<MovieDeserialized>();
                foreach (MovieDeserialized movieDeserialized in movieCategoryDeserialized.Movies)
                {
                    movieDeserialized.IsChecked = true;
                    _selectedMovieIds.Add(movieDeserialized.Id);
                    movies.Add(movieDeserialized);
                }
                foreach (Movie movie in rawMovies)
                {
                    if (_selectedMovieIds.Contains(movie.Id))
                    {
                        continue;
                    }

                    try
                    {
                        MovieDeserialized movieDeserialized = new MovieDeserialized(movie);
                        movies.Add(movieDeserialized);
                    }
                    catch (Exception ex)
                    {
                        Messages.Error(ex.Message, ref Splash);
                        _callback();
                    }
                }
                movies.Sort();
                lvMovieList.ItemsSource = movies;
            }

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvMovieList.ItemsSource);
            view.Filter = MovieFilter;
            txtFilter.IsReadOnly = false;
            txtFilter.Focusable = true;
            txtFilter.IsHitTestVisible = true;

            txtCategoryName.Focus();
        }

        // Close the window on cancel
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _splash.Visibility = Visibility.Collapsed;
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
            parentWindow.removeChild(this);
            Close();
        }

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtCategoryName.Text == "")
            {
                Messages.ShowOKMessageBox("You need to enter a category name", ref Splash);
            }
            else
            { 
                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<MovieCategory>();
                    List<MovieCategory> categories = connection.Table<MovieCategory>().ToList();
                    foreach (MovieCategory movieCategory in categories)
                    {
                        if (movieCategory.Name != _originalCategoryName && movieCategory.Name == txtCategoryName.Text.ToUpper())
                        {
                            repeat = true;
                            break;
                        }
                    }

                    if (repeat)
                    {
                        Messages.ShowOKMessageBox("A category with that name already exists", ref Splash);
                    }
                    else
                    {
                        MovieCategory result = connection.Get<MovieCategory>((int)Tag);
                        _selectedMovieIds.Sort();
                        DatabaseFunctions.UpdateCategoryInMovies(result.Name, txtCategoryName.Text.ToUpper(), _selectedMovieIds);
                        result.Name = txtCategoryName.Text.ToUpper();
                        result.MovieIds = JsonConvert.SerializeObject(_selectedMovieIds);
                        connection.Update(result);
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

        // Scale based on the size of the window
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<UpdateMovieCategory>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(updateMovieCategoryWindow, 500f, 350f);
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
