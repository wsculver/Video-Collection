using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCollection.Shows
{
    public class ShowVideo : IComparable
    {
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public string Thumbnail { get; set; }
        public string FilePath { get; set; }
        // JSON encoded List<ShowVideo>
        public string Commentaries { get; set; }
        // JSON encoded ShowVideo
        public string DeletedScenes { get; set; }
        public string Section { get; set; }
        public string Runtime { get; set; }
        // JSON encoded List<SubtitleSegment>
        public string Subtitles { get; set; }

        public int CompareTo(object obj)
        {
            ShowVideo video = obj as ShowVideo;
            return EpisodeNumber.CompareTo(video.EpisodeNumber);
        }
    }
}
