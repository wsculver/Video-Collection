using System.Collections.Generic;
using Newtonsoft.Json;

namespace VideoCollection.Shows
{
    public class ShowSeasonDeserialized
    {
        public int SeasonNumber { get; set; }
        public string SeasonName { get; set; }
        public List<ShowSectionDeserialized> Sections { get; set; }
        public List<ShowVideoDeserialized> Videos { get; set; }

        public ShowSeasonDeserialized(ShowSeason show)
        {
            SeasonNumber = show.SeasonNumber;
            SeasonName = show.SeasonName;
            List<ShowSection> showSections = JsonConvert.DeserializeObject<List<ShowSection>>(show.Sections);
            List<ShowSectionDeserialized> showSectionsDeserialized = new List<ShowSectionDeserialized>();
            foreach (ShowSection section in showSections)
            {
                showSectionsDeserialized.Add(new ShowSectionDeserialized(section));
            }
            Sections = showSectionsDeserialized;
            List<ShowVideo> showVideos = JsonConvert.DeserializeObject<List<ShowVideo>>(show.Videos);
            List<ShowVideoDeserialized> showVideosDeserialized = new List<ShowVideoDeserialized>();
            foreach (ShowVideo video in showVideos)
            {
                showVideosDeserialized.Add(new ShowVideoDeserialized(video));
            }
            Videos = showVideosDeserialized;
        }
    }
}
