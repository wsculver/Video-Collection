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
using VideoCollection.Helpers;
using VideoCollection.Movies;

namespace VideoCollection.Popups
{
    /// <summary>
    /// Interaction logic for MovieDetails.xaml
    /// </summary>
    public partial class MovieDetails : Window
    {
        private MovieDeserialized _movieDeserialized;

        // Don't use this constructur. It is only here to make resizing work
        public MovieDetails() { }

        public MovieDetails(string Id)
        {
            InitializeComponent();

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Movie>();
                Movie movie = connection.Query<Movie>("SELECT * FROM Movie WHERE Id = " + Id)[0];
                _movieDeserialized = new MovieDeserialized(movie.Id, movie.Title, movie.Thumbnail, movie.MovieFilePath, movie.BonusFolderPath, movie.Categories, false);
                labelTitle.Content = _movieDeserialized.Title.ToUpper();
                imageMovieThumbnail.Source = _movieDeserialized.Thumbnail;
            }
        }

        // Scale based on the size of the window
        private static ScaleValueHelper _scaleValueHelper = new ScaleValueHelper();
        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = _scaleValueHelper.SetScaleValueProperty<MovieDetails>();
        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion
        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            ScaleValue = _scaleValueHelper.CalculateScale(movieDetailsWindow, 400f, 700f);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void imageMovieThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void imageThumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
