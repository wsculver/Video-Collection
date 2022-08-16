using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows;
using VideoCollection.Database;
using VideoCollection.Popups;

namespace VideoCollection.Movies
{
    public class MovieCategoryDeserialized : IComparable
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        public List<MovieDeserialized> Movies { get; set; }
        public bool IsChecked { get; set; }
        public bool IsEnabled { get; set; }
        public HashSet<string> AllCategories { get; set; }

        public MovieCategoryDeserialized(MovieCategory category)
        {
            AllCategories = DatabaseFunctions.MovieAllCategories;
            Id = category.Id;
            Position = category.Position;
            Name = category.Name;
            SortedSet<int> movieIds = JsonConvert.DeserializeObject<SortedSet<int>>(category.MovieIds);
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
            if (AllCategories.Contains(category.Name.ToUpper()))
            {
                IsChecked = true;
                IsEnabled = false;
            }
            else
            {
                IsChecked = false;
                IsEnabled = true;
            }
        }

        public MovieCategoryDeserialized(int id, int position, string name, List<MovieDeserialized> movies, bool check)
        {
            AllCategories = DatabaseFunctions.MovieAllCategories;
            Id = id;
            Position = position;
            Name = name;
            Movies = movies;
            if (AllCategories.Contains(name.ToUpper()))
            {
                IsChecked = true;
                IsEnabled = false;
            }
            else
            {
                IsChecked = check;
                IsEnabled = true;
            }
        }

        public int CompareTo(object obj)
        {
            MovieCategoryDeserialized c = obj as MovieCategoryDeserialized;
            
            if (AllCategories.Contains(c.Name.ToUpper()))
            {
                return 1;
            } 
            else if (AllCategories.Contains(Name.ToUpper()))
            {
                return -1;
            }
            else
            {
                StringComparer comparer = StringComparer.OrdinalIgnoreCase;
                return comparer.Compare(Name, c.Name);
            }
        }
    }
}
