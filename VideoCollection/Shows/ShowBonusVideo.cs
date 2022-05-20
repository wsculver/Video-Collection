using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCollection.Shows
{
    public class ShowBonusVideo
    {
        public string Title { get; set; }
        public string Thumbnail { get; set; }
        public string FilePath { get; set; }
        public string Section { get; set; }
        public string Runtime { get; set; }
        // JSON encoded List<SubtitleSegment>
        public string Subtitles { get; set; }
    }
}
