using SQLite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCollection.Shows
{
    public class ShowEpisode : IComparable
    {
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public string Thumbnail { get; set; }
        public string FilePath { get; set; }
        // JSON encoded ShowSeasonDeserialized
        public string Season { get; set; }
        public string Runtime { get; set; }
        // JSON encoded List<SubtitleSegment>
        public string Subtitles { get; set; }

        public int CompareTo(object obj)
        {
            ShowEpisode m = obj as ShowEpisode;
            return EpisodeNumber.CompareTo(m.EpisodeNumber);
        }
    }
}
