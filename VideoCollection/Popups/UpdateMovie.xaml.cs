﻿using Microsoft.Win32;
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
        private int _movieId;
        private bool _changesSaved;

        public UpdateMovie()
        {
            InitializeComponent();

            _selectedCategories = new List<string>();
            _changesSaved = false;

            UpdateMovieList();

            // Load categories
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                List<MovieCategory> rawCategories = (connection.Table<MovieCategory>().ToList()).OrderBy(c => c.Name).ToList();
                List<MovieCategoryDeserialized> categories = new List<MovieCategoryDeserialized>();
                foreach (MovieCategory category in rawCategories)
                {
                    categories.Add(new MovieCategoryDeserialized(category.Id, category.Position, category.Name, category.Movies, category.IsChecked));
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
                    movies.Add(new MovieDeserialized(movie.Id, movie.Title, movie.Thumbnail, movie.MovieFilePath, movie.BonusFolderPath, movie.Categories, false));
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

        // Convert Uri into an ImageSource
        private ImageSource BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(source.AbsoluteUri);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
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
            if (txtMovieName.Text != "")
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    // Update the movie in the Movie table
                    connection.CreateTable<Movie>();
                    Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + _movieId)[0];
                    bool movieContentChanged = MovieContentChanged(movie);
                    movie.Title = txtMovieName.Text;
                    movie.Thumbnail = imgThumbnail.Source.ToString();
                    movie.MovieFilePath = txtFile.Text;
                    movie.BonusFolderPath = txtBonusFolder.Text;
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
                            if(movieContentChanged)
                            {
                                DatabaseFunctions.UpdateMovieInCategory(movie, category);
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
    }
}