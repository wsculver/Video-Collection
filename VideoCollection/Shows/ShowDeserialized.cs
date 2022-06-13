using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using VideoCollection.Helpers;
using VideoCollection.Subtitles;
using VideoCollection.Popups;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using VideoCollection.Database;
using Newtonsoft.Json;
using SQLite;

namespace VideoCollection.Shows
{
    public class ShowDeserialized
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ShowFolderPath { get; set; }
        public ImageSource Thumbnail { get; set; }
        public List<ShowSeasonDeserialized> Seasons { get; set; }
        public ShowVideoDeserialized NextEpisode { get; set; }
        public string Rating { get; set; }
        public List<string> Categories { get; set; }
        public bool IsChecked { get; set; }

        public ShowDeserialized(Show show)
        {
            Id = show.Id;
            Title = show.Title;
            ShowFolderPath = show.ShowFolderPath;
            Thumbnail = StaticHelpers.Base64ToImageSource(show.Thumbnail);
            List<ShowSeason> showSeasons = JsonConvert.DeserializeObject<List<ShowSeason>>(show.Seasons);
            List<ShowSeasonDeserialized> showSeasonsDeserialized = new List<ShowSeasonDeserialized>();
            foreach (ShowSeason season in showSeasons)
            {
                showSeasonsDeserialized.Add(new ShowSeasonDeserialized(season));
            }
            Seasons = showSeasonsDeserialized;
            ShowVideo nextEpisode = JsonConvert.DeserializeObject<ShowVideo>(show.NextEpisode);
            NextEpisode = new ShowVideoDeserialized(nextEpisode);
            Rating = show.Rating;
            Categories = JsonConvert.DeserializeObject<List<string>>(show.Categories);
            IsChecked = show.IsChecked;
        }



        // Update the next episode for a show
        public void UpdateNextEpisode()
        {
            int seasonNum = NextEpisode.NextEpisode.Item1;
            int episodeNum = NextEpisode.NextEpisode.Item2;

            NextEpisode = Seasons.ElementAt(seasonNum).Videos.ElementAt(episodeNum);

            Show show = new Show(this);

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Show>();
                connection.Update(show);
                /*List<ShowCategory> categories = (connection.Table<ShowCategory>().ToList()).OrderBy(c => c.Name).ToList();
                foreach (ShowCategory category in categories)
                {
                    if (this.Categories.Contains(category.Name))
                    {
                        connection.Update(category);
                    }
                }*/
            }
        }
    }
}
