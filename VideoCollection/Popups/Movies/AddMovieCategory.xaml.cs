﻿using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VideoCollection.CustomTypes;
using VideoCollection.Helpers;
using VideoCollection.Movies;

namespace VideoCollection.Popups.Movies
{
    /// <summary>
    /// Interaction logic for AddMovieCategory.xaml
    /// </summary>
    public partial class AddMovieCategory : Window, ScaleableWindow
    {
        private List<int> _selectedMovieIds;
        private Border _splash;
        private Action _callback;

        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        /// <summary> Don't use this constructur. It is only here to make resizing work </summary>
        public AddMovieCategory() { }

        public AddMovieCategory(ref Border splash, Action callback)
        {
            InitializeComponent();

            Closed += (a, b) => { Owner.Activate(); };

            _splash = splash;
            _callback = callback;
            _selectedMovieIds = new List<int>();

            UpdateMovieList();

            txtCategoryName.Focus();

            WidthScale = 0.35;
            HeightScale = 0.85;
            HeightToWidthRatio = 1.299;
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
                        _callback();
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

        // Close window on cancel
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
                        if (movieCategory.Name == txtCategoryName.Text.ToUpper())
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
                        connection.CreateTable<Movie>();
                        foreach (int id in _selectedMovieIds)
                        {
                            Movie movie = connection.Get<Movie>(id);

                            // Add category to selected movie
                            List<string> movieCategories = JsonConvert.DeserializeObject<List<string>>(movie.Categories);
                            movieCategories.Add(txtCategoryName.Text.ToUpper());
                            movie.Categories = JsonConvert.SerializeObject(movieCategories);
                            connection.Update(movie);
                        }

                        _selectedMovieIds.Sort();

                        MovieCategory category = new MovieCategory()
                        {
                            Name = txtCategoryName.Text.ToUpper(),
                            MovieIds = JsonConvert.SerializeObject(_selectedMovieIds),
                            IsChecked = false
                        };

                        connection.Query<MovieCategory>("CREATE TRIGGER IF NOT EXISTS updatePosition AFTER INSERT ON MovieCategory BEGIN UPDATE MovieCategory SET Position = new.Id WHERE Id = new.Id; END; ");
                        connection.Insert(category);
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
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = ScaleValueHelper.SetScaleValueProperty<AddMovieCategory>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = ScaleValueHelper.CalculateScale(addMovieCategoryWindow, 500f, 350f);
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
