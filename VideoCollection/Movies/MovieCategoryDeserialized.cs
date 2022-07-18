using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows;
using VideoCollection.Database;
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
            List<int> movieIds = JsonConvert.DeserializeObject<List<int>>(category.MovieIds);
            List<MovieDeserialized> moviesDeserialized = new List<MovieDeserialized>();
            foreach (int id in movieIds)
            {
                try
                {
                    moviesDeserialized.Add(new MovieDeserialized(DatabaseFunctions.GetMovie(id)));
                }
                catch (Exception ex)
                {
                    MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;
                    CustomMessageBox popup = new CustomMessageBox("Error: " + ex.Message, CustomMessageBox.MessageBoxType.OK);
                    popup.scaleWindow(parentWindow);
                    parentWindow.addChild(popup);
                    popup.Owner = parentWindow;
                    parentWindow.Splash.Visibility = Visibility.Visible;
                    popup.ShowDialog();
                    parentWindow.Splash.Visibility = Visibility.Collapsed;
                }
            }
            moviesDeserialized.Sort();
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
