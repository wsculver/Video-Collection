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

namespace VideoCollection.Shows
{
    public class ShowEpisodeDeserialized
    {
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string FilePath { get; set; }
        public ShowSeasonDeserialized Season { get; set; }
        public string Runtime { get; set; }
        public string SubtitlesSerialized { get; set; }
        public List<SubtitleSegment> Subtitles { get; set; }

        public ShowEpisodeDeserialized(ShowEpisode showEpisode)
        {
            EpisodeNumber = showEpisode.EpisodeNumber;
            Title = showEpisode.Title;
            FilePath = showEpisode.FilePath;
            Thumbnail = StaticHelpers.Base64ToImageSource(showEpisode.Thumbnail);
            ShowSeason season = JsonConvert.DeserializeObject<ShowSeason>(showEpisode.Season);
            Season = new ShowSeasonDeserialized(season);
        }
    }
}
