using Newtonsoft.Json;
using System.Collections.Generic;
using System.Windows.Media;
using VideoCollection.Helpers;
using VideoCollection.Subtitles;

namespace VideoCollection.Movies
{
    public class MovieBonusVideoDeserialized
    {
        public string Title { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string FilePath { get; set; }
        public string Section { get; set; }
        public string Runtime { get; set; }
        public string SubtitlesSerialized { get; set; }
        public List<SubtitleSegment> Subtitles { get; set; }

        public MovieBonusVideoDeserialized(MovieBonusVideo video)
        {
            Title = video.Title;
            Thumbnail = StaticHelpers.Base64ToImageSource(video.Thumbnail);
            FilePath = video.FilePath;
            Section = video.Section;
            Runtime = video.Runtime;
            SubtitlesSerialized = video.Subtitles;
            Subtitles = JsonConvert.DeserializeObject<List<SubtitleSegment>>(video.Subtitles);
        }
    }
}
