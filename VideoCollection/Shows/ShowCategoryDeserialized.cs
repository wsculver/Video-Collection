using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
            List<Show> showsList = JsonConvert.DeserializeObject<List<Show>>(category.Shows);
            List<ShowDeserialized> showsDeserialized = new List<ShowDeserialized>();
            foreach (Show show in showsList)
            {
                try
                {
                    showsDeserialized.Add(new ShowDeserialized(show));
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
