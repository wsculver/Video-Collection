using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows;
using VideoCollection.Database;
using VideoCollection.Popups;

namespace VideoCollection.Shows
{
    public class ShowCategoryDeserialized
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        public List<ShowDeserialized> Shows { get; set; }
        public bool IsChecked { get; set; }

        public ShowCategoryDeserialized(ShowCategory category)
        {
            Id = category.Id;
            Position = category.Position;
            Name = category.Name;
            List<int> showIds = JsonConvert.DeserializeObject<List<int>>(category.ShowIds);
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
            Shows = showsDeserialized;
            IsChecked = category.IsChecked;
        }

        public ShowCategoryDeserialized(int id, int position, string name, List<ShowDeserialized> shows, bool check)
        {
            Id = id;
            Position = position;
            Name = name;
            Shows = shows;
            IsChecked = check;
        }
    }
}
