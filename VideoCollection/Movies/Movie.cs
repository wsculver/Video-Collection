using SQLite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCollection.Movies
{
    internal class Movie : IComparable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Title { get; set; }
        public string MovieFolderPath { get; set; }
        public string Thumbnail { get; set; }
        public string MovieFilePath { get; set; }
        public string Runtime { get; set; }
        // JSON encoded List<string>
        public string BonusSections { get; set; }
        // JSON encoded List<MovieBonusVideo>
        public string BonusVideos { get; set; }
        public string Rating { get; set; }
        // JSON encoded List<string>
        public string Categories { get; set; }
        // Used for editing categories
        public bool IsChecked { get; set; }

        public int CompareTo(object obj)
        {
            Movie m = obj as Movie;
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(Title, m.Title);
        }
    }
}
