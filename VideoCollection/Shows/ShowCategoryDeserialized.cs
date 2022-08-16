using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows;
using VideoCollection.Database;
using VideoCollection.Popups;

namespace VideoCollection.Shows
{
    public class ShowCategoryDeserialized : IComparable
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        public List<ShowDeserialized> Shows { get; set; }
        public bool IsChecked { get; set; }
        public bool IsEnabled { get; set; }
        public HashSet<string> AllCategories { get; set; }

        public ShowCategoryDeserialized(ShowCategory category)
        {
            AllCategories = DatabaseFunctions.ShowAllCategories;
            Id = category.Id;
            Position = category.Position;
            Name = category.Name;
            SortedSet<int> showIds = JsonConvert.DeserializeObject<SortedSet<int>>(category.ShowIds);
            List<ShowDeserialized> showsDeserialized = new List<ShowDeserialized>();
            foreach (int id in showIds)
            {
                try
                {
                    showsDeserialized.Add(new ShowDeserialized(DatabaseFunctions.GetShow(id)));
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
            showsDeserialized.Sort();
            Shows = showsDeserialized;
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

        public ShowCategoryDeserialized(int id, int position, string name, List<ShowDeserialized> shows, bool check)
        {
            AllCategories = DatabaseFunctions.ShowAllCategories;
            Id = id;
            Position = position;
            Name = name;
            Shows = shows;
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
            ShowCategoryDeserialized c = obj as ShowCategoryDeserialized;

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
