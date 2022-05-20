using SQLite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCollection.Shows
{
    public class Show : IComparable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Title { get; set; }
        public string ShowFolderPath { get; set; }
        public string Thumbnail { get; set; }
        // JSON encoded List<ShowSeasonDeserialized>
        public string Seasons { get; set; }
        // JSON encoded List<string>
        public string BonusSections { get; set; }
        // JSON encoded List<ShowBonusVideo>
        public string BonusVideos { get; set; }
        public string Rating { get; set; }
        // JSON encoded List<string>
        public string Categories { get; set; }
        // Used for editing categories
        public bool IsChecked { get; set; }

        public int CompareTo(object obj)
        {
            Show m = obj as Show;
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(Title, m.Title);
        }
    }
}
