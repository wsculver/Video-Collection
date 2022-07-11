using SQLite;
using System;

namespace VideoCollection.Movies
{
    public class Movie
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Title { get; set; }
        public string MovieFolderPath { get; set; }
        public string Thumbnail { get; set; }
        public string ThumbnailVisibility { get; set; }
        public string MovieFilePath { get; set; }
        public string Runtime { get; set; }
        // JSON encoded List<string>
        public string BonusSections { get; set; }
        // JSON encoded List<MovieBonusVideo>
        public string BonusVideos { get; set; }
        public string Rating { get; set; }
        // JSON encoded List<string>
        public string Categories { get; set; }
        // JSON encoded List<SubtitleSegment>
        public string Subtitles { get; set; }
    }
}
