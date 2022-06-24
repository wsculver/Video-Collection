using SQLite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoCollection.Helpers;
using Newtonsoft.Json;

namespace VideoCollection.Shows
{
    public class Show : IComparable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Title { get; set; }
        public string ShowFolderPath { get; set; }
        public string Thumbnail { get; set; }
        public string ThumbnailVisibility { get; set; }
        // JSON encoded List<ShowSeason>
        public string Seasons { get; set; }
        // JSON encoded ShowVideo
        public string NextEpisode { get; set; }
        public string Rating { get; set; }
        // JSON encoded List<string>
        public string Categories { get; set; }
        // Used for editing categories
        public bool IsChecked { get; set; }

        public Show() { }

        public Show(ShowDeserialized show)
        {
            Id = show.Id;
            Title = show.Title;
            ShowFolderPath = show.ShowFolderPath;
            Thumbnail = StaticHelpers.ImageSourceToBase64(show.Thumbnail);
            ThumbnailVisibility = show.ThumbnailVisibility;
            List<ShowSeason> seasons = new List<ShowSeason>();
            foreach(var season in show.Seasons)
            {
                seasons.Add(new ShowSeason(season));
            }
            Seasons = JsonConvert.SerializeObject(seasons);
            NextEpisode = JsonConvert.SerializeObject(new ShowVideo(show.NextEpisode));
            Rating = show.Rating;
            Categories = JsonConvert.SerializeObject(show.Categories);
            IsChecked = false;
        }

        public int CompareTo(object obj)
        {
            Show m = obj as Show;
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(Title, m.Title);
        }
    }
}
