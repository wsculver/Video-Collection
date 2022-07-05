using System;

namespace VideoCollection.Subtitles
{
    public class SubtitleSegment : IComparable<SubtitleSegment>
    {
        public string Start { get; set; }
        public string End { get; set; }
        public string Content { get; set; }

        public int CompareTo(SubtitleSegment other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if(ReferenceEquals(null, other)) return 1;
            var startComarison = Start.CompareTo(other.Start);
            if (startComarison != 0) return startComarison;
            return End.CompareTo(other.End);
        }
    }
}
