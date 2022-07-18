using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            ConcurrentBag<ShowVideoDeserialized> showVideosDeserialized = new ConcurrentBag<ShowVideoDeserialized>();
            Parallel.ForEach(showVideos, video =>
            {
                showVideosDeserialized.Add(new ShowVideoDeserialized(video));
            });
            List<ShowVideoDeserialized> showVideosDeserializedList = showVideosDeserialized.ToList();
            showVideosDeserializedList.Sort();
            Videos = showVideosDeserializedList;
        }
    }
}
