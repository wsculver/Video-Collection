using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VideoCollection.Popups;

namespace VideoCollection.Movies
{
    public class MovieCategoryDeserialized
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        public List<MovieDeserialized> Movies { get; set; }
        public bool IsChecked { get; set; }

        public MovieCategoryDeserialized(MovieCategory category)
        {
            Id = category.Id;
            Position = category.Position;
            Name = category.Name;
            List<Movie> moviesList = JsonConvert.DeserializeObject<List<Movie>>(category.Movies);
            List<MovieDeserialized> moviesDeserialized = new List<MovieDeserialized>();
            foreach (Movie movie in moviesList)
            {
                try
                {
                    moviesDeserialized.Add(new MovieDeserialized(movie));
                }
                catch (Exception ex)
                {
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    CustomMessageBox popup = new CustomMessageBox("Error: " + ex.Message, CustomMessageBox.MessageBoxType.OK);
                    popup.Width = parentWindow.ActualWidth * 0.25;
                    popup.Height = popup.Width * 0.55;
                    popup.Owner = parentWindow;
                    parentWindow.Splash.Visibility = Visibility.Visible;
                    popup.ShowDialog();
                    parentWindow.Splash.Visibility = Visibility.Collapsed;
                }
            }
            Movies = moviesDeserialized;
            IsChecked = category.IsChecked;
        }

        public MovieCategoryDeserialized(int id, int position, string name, List<MovieDeserialized> movies, bool check)
        {
            Id = id;
            Position = position;
            Name = name;
            Movies = movies;
            IsChecked = check;
        }
    }
}
