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
    public class ShowSeasonDeserialized
    {
        public int SeasonNumber { get; set; }
        public List<ShowBonusSectionDeserialized> BonusSections { get; set; }
        public List<ShowBonusVideoDeserialized> BonusVideos { get; set; }

        public ShowSeasonDeserialized(ShowSeason show)
        {
            SeasonNumber = show.SeasonNumber;
            List<ShowBonusSection> showBonusSections = JsonConvert.DeserializeObject<List<ShowBonusSection>>(show.BonusSections);
            List<ShowBonusSectionDeserialized> showBonusSectionsDeserialized = new List<ShowBonusSectionDeserialized>();
            foreach (ShowBonusSection section in showBonusSections)
            {
                showBonusSectionsDeserialized.Add(new ShowBonusSectionDeserialized(section));
            }
            BonusSections = showBonusSectionsDeserialized;
            List<ShowBonusVideo> showBonusVideos = JsonConvert.DeserializeObject<List<ShowBonusVideo>>(show.BonusVideos);
            List<ShowBonusVideoDeserialized> showBonusVideosDeserialized = new List<ShowBonusVideoDeserialized>();
            foreach (ShowBonusVideo video in showBonusVideos)
            {
                showBonusVideosDeserialized.Add(new ShowBonusVideoDeserialized(video));
            }
            BonusVideos = showBonusVideosDeserialized;
        }
    }
}
