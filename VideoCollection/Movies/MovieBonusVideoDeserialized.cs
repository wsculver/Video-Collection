using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using VideoCollection.Helpers;

namespace VideoCollection.Movies
{
    public class MovieBonusVideoDeserialized
    {
        public string Title { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string FilePath { get; set; }
        public string Section { get; set; }

        public MovieBonusVideoDeserialized(MovieBonusVideo video)
        {
            Title = video.Title;
            Thumbnail = StaticHelpers.Base64ToImageSource(video.Thumbnail);
            FilePath = video.FilePath;
            Section = video.Section;
        }
    }
}
