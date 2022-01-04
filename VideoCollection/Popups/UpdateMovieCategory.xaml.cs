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
using VideoCollection.Helpers;
using VideoCollection.Movies;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for RenameMovieCategory.xaml
    /// </summary>
    public partial class UpdateMovieCategory : Window
    {
        private List<int> _selectedMovieIds;
        private string _originalCategoryName;

        // Don't use this constructur. It is only here to make resizing work
        public UpdateMovieCategory() { }

        public UpdateMovieCategory(string Id)
        {
            InitializeComponent();

            _selectedMovieIds = new List<int>();

            Tag = Id;

            // Check current movies and fill the category name text box
            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<MovieCategory>();
                MovieCategory movieCategory = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + Tag.ToString())[0];
                txtCategoryName.Text = movieCategory.Name;
                _originalCategoryName = movieCategory.Name;
                MovieCategoryDeserialized movieCategoryDeserialized = new MovieCategoryDeserialized(movieCategory);

                connection.CreateTable<Movie>();
                List<Movie> rawMovies = (connection.Table<Movie>().ToList()).OrderBy(c => c.Title).ToList();
                List<MovieDeserialized> movies = new List<MovieDeserialized>();
                foreach (Movie movie in rawMovies)
                {
                    bool check = false;
                    foreach (MovieDeserialized movieDeserialized in movieCategoryDeserialized.Movies)
                    {
                        if (movieDeserialized.Id == movie.Id)
                        {
                            check = true;
                            _selectedMovieIds.Add(movie.Id);
                        }
                    }

                    movie.IsChecked = check;
                    movies.Add(new MovieDeserialized(movie));
                }
                lvMovieList.ItemsSource = movies;
            }
        }

        // Close the window on cancel
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Shows a custom OK message box
        private void ShowOKMessageBox(string message)
        {
            Window parentWindow = GetWindow(this).Owner;
            CustomMessageBox popup = new CustomMessageBox(message, CustomMessageBox.MessageBoxType.OK);
            popup.Width = parentWindow.Width * 0.25;
            popup.Height = parentWindow.Height * 0.25;
            popup.Owner = parentWindow;
            Splash.Visibility = Visibility.Visible;
            popup.ShowDialog();
            Splash.Visibility = Visibility.Collapsed;
        }

        // Save entered info
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtCategoryName.Text == "")
            {
                ShowOKMessageBox("You need to enter a category name");
            }
            else
            { 
                JavaScriptSerializer jss = new JavaScriptSerializer();

                bool repeat = false;

                using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
                {
                    connection.CreateTable<MovieCategory>();
                    List<MovieCategory> categories = connection.Table<MovieCategory>().ToList();
                    foreach (MovieCategory movieCategory in categories)
                    {
                        if (movieCategory.Name != _originalCategoryName && movieCategory.Name == txtCategoryName.Text.ToUpper())
                            repeat = true;
                    }

                    if (repeat)
                    {
                        ShowOKMessageBox("A category with that name already exists");
                    }
                    else
                    {

                        List<Movie> selectedMovies = new List<Movie>();

                        foreach (int id in _selectedMovieIds)
                        {
                            Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + id.ToString())[0];
                            selectedMovies.Add(movie);
                        }

                        MovieCategory result = connection.Query<MovieCategory>("SELECT * FROM MovieCategory WHERE Id = " + Tag.ToString())[0];
                        DatabaseFunctions.UpdateCategoryNameInMovies(result.Name, txtCategoryName.Text.ToUpper());
                        result.Name = txtCategoryName.Text.ToUpper();
                        result.Movies = jss.Serialize(selectedMovies);
                        connection.Update(result);
                    }
                }

                if (!repeat)
                {
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
        private static ScaleValueHelper _scaleValueHelper = new ScaleValueHelper();
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = _scaleValueHelper.SetScaleValueProperty<UpdateMovieCategory>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = _scaleValueHelper.CalculateScale(updateMovieCategoryWindow, 500f, 300f);
        }
    }
}
