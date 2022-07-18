using SQLite;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace VideoCollection.Shows
{
    public class Show
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

        public List<ShowSeasonDeserialized> getSeasons()
        {
            List<ShowSeason> showSeasons = JsonConvert.DeserializeObject<List<ShowSeason>>(Seasons);
            List<ShowSeasonDeserialized> showSeasonsDeserialized = new List<ShowSeasonDeserialized>();
            foreach (ShowSeason season in showSeasons)
            {
                showSeasonsDeserialized.Add(new ShowSeasonDeserialized(season));
            }
            return showSeasonsDeserialized;
        }

        // Update the next episode for a show
        public void UpdateNextEpisode()
        {
            NextEpisode = JsonConvert.SerializeObject(GetNextEpisode());

            using (SQLiteConnection connection = new SQLiteConnection(App.databasePath))
            {
                connection.CreateTable<Show>();
                connection.Update(this);
            }
        }

        public ShowVideo GetNextEpisode()
        {
            ShowVideoDeserialized nextEpisode = new ShowVideoDeserialized(JsonConvert.DeserializeObject<ShowVideo>(NextEpisode));
            int seasonNum = nextEpisode.NextEpisode.Item1;
            int episodeNum = nextEpisode.NextEpisode.Item2;

            return GetEpisode(seasonNum, episodeNum);
        }

        public ShowVideo GetEpisode(int seasonIndex, int episodeIndex)
        {
            List<ShowSeason> showSeasons = JsonConvert.DeserializeObject<List<ShowSeason>>(Seasons);
            int showSeasonsLength = showSeasons.Count();
            ShowSeasonDeserialized showSeasonDeserialized = new ShowSeasonDeserialized(showSeasons.ElementAt(seasonIndex));
            ShowVideoDeserialized showVideo = showSeasonDeserialized.Videos.OrderByDescending(x => x.IsBonusVideo).ThenBy(x => x.EpisodeNumber).ToList().ElementAt(episodeIndex);
            return new ShowVideo(showVideo);
        }
    }
}
