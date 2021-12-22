﻿using SQLite;
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
using VideoCollection.Helpers;
using VideoCollection.Movies;
using VideoCollection.Views;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for AddMovieCategory.xaml
    /// </summary>
    public partial class AddMovieCategory : Window
    {
        private List<int> _selectedMovieIds;

        public AddMovieCategory()
        {
            InitializeComponent();

            _selectedMovieIds = new List<int>();

            UpdateMovieList();
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

        // Close window on cancel
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtCategoryName.Text != "") 
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();

                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    List<Movie> selectedMovies = new List<Movie>();

                    foreach(int id in _selectedMovieIds)
                    {
                        Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + id.ToString())[0];
                        selectedMovies.Add(movie);
                    }

                    MovieCategory category = new MovieCategory()
                    {
                        Name = txtCategoryName.Text.ToUpper(),
                        Movies = jss.Serialize(selectedMovies),
                        IsChecked = false
                    };

                    List<MovieCategory> categories = connection.Table<MovieCategory>().ToList();
                    foreach(MovieCategory movieCategory in categories)
                    {
                        if(movieCategory.Name == category.Name)
                            repeat = true;
                    }

                    if(repeat)
                    {
                        MessageBox.Show("A category with that name already exists", "Duplicate Category", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        connection.CreateTable<MovieCategory>();
                        connection.Query<MovieCategory>("CREATE TRIGGER IF NOT EXISTS updatePosition AFTER INSERT ON MovieCategory BEGIN UPDATE MovieCategory SET Position = new.Id WHERE Id = new.Id; END; ");
                        connection.Insert(category);
                    }
                }

                if (!repeat)
                {
                    Close();
                }
            }
            else
            {
                MessageBox.Show("You need to enter a category name", "Missing Category Name", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Select a movie
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _selectedMovieIds.Add((int)(sender as CheckBox).Tag);
        }

        // Unselect a movie
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _selectedMovieIds.Remove((int)(sender as CheckBox).Tag);
        }

        // Scale based on the size of the window
        private static ScaleValueHelper _scaleValueHelper = new ScaleValueHelper();
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = _scaleValueHelper.SetScaleValueProperty<AddMovieCategory>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = _scaleValueHelper.CalculateScale(addMovieCategoryWindow, 500f, 300f);
        }
    }
}